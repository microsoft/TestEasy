using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Microsoft.Web.Administration;
using TestEasy.Core;
using TestEasy.Core.Configuration;

namespace TestEasy.WebServer
{
    /// <summary>
    ///     Represents IISExpress web server and provides helper APIs
    /// </summary>
    public class WebServerIisExpress : WebServer, IIisCompatibleWebServer
    {
        internal const string CommandLineArgsFormat = "/config:\"{0}\"";
        public const string PathToIssexpress = @"%PROGRAMFILES%\IIS Express\iisexpress.exe";

        private readonly Architecture _architecture;
        private readonly int _httpPort;
        private readonly int _startupTimeout;
        private readonly string _serverExecutablePath;
        private readonly string _applicationHostConfigPath;
        private Process _serverProcess;
        private IisExpressConfig _iisExpressConfig;

        /// <summary>
        ///     ctor
        /// </summary>
        public WebServerIisExpress()
            : this(new WebServerSettings())
        {
        }

        /// <summary>
        ///     ctor
        /// </summary>
        public WebServerIisExpress(WebServerSettings settings)
            : this(settings, Dependencies.Instance)
        {
        }

        /// <summary>
        ///     ctor
        /// </summary>
        public WebServerIisExpress(WebServerSettings settings, Dependencies dependencies)
        {
            if (TestEasyConfig.Instance.Client.Remote)
            {
                throw new NotSupportedException("For tests using remote browsers, IISExpress web server type is not supported, please use IIS.");
            }

            if (string.IsNullOrEmpty(settings.RootPhysicalPath) && !string.IsNullOrEmpty(settings.WebAppName))
            {
                settings.RootPhysicalPath = TestEasyHelpers.Tools.GetUniqueTempPath(settings.WebAppName);
            }

            _architecture = settings.Architecture;
            _fileSystem = dependencies.FileSystem;
            _environmentSystem = dependencies.EnvironmentSystem;
            _processRunner = dependencies.ProcessRunner;
            _serverManagerProvider = dependencies.ServerManagerProvider;

            _type = "IISExpress";
            _hostName = settings.HostName;
            _startupTimeout = settings.StartupTimeout;
            _rootPhysicalPath = string.IsNullOrEmpty(settings.RootPhysicalPath)
                ? Path.GetDirectoryName(_fileSystem.GetExecutingAssemblyDirectory())
                : settings.RootPhysicalPath;
            _httpPort = _environmentSystem.GetNextAvailablePort(0);
            _serverExecutablePath = LocateIisExpress(); // _version is initialized here
            _applicationHostConfigPath = CreateApplicationHostConfigFromTemplate();

            _configs = new Dictionary<string, string>
            {
                {
                    "applicationhost.config", _applicationHostConfigPath
                }
            };

            _iisExpressConfig = new IisExpressConfig(_applicationHostConfigPath, dependencies.FileSystem);
            
            InitializeApplicationHostConfig();

            Start();
        }

        protected override void DisposeWebServer(bool disposing)
        {
            if (IsRunning)
            {
                // dispose managed resources
                Stop();
            }
        }

        private bool IsRunning
        {
            get
            {
                return (_serverProcess != null);
            }
        }

        /// <summary>
        ///     Start IISExpress process and point it to a copy of applicationhost.config
        /// </summary>
        public override sealed void Start()
        {
            // prepare command line params
            var arguments = string.Format(CommandLineArgsFormat, _applicationHostConfigPath);

            // prepare process parameters
            var psi = new ProcessStartInfo
                {
                    FileName = _serverExecutablePath,
                    WorkingDirectory = _rootPhysicalPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments = arguments
                };

            _serverProcess = new Process { StartInfo = psi };

            // try to start process
            if (!_processRunner.Start(_serverProcess))
            {
                _serverProcess = null;
                throw new Exception(string.Format("Failed to start process '{0} {1}'.", _serverExecutablePath, arguments));
            }

            // wait for process some time - it also gives us some delay before we start requesting 
            if (_processRunner.WaitForExit(_serverProcess, _startupTimeout))
            {
                _serverProcess = null;
                throw new Exception(string.Format("Process '{0} {1}' exited unexpctedly.", _serverExecutablePath, arguments));
            }
        }

        /// <summary>
        ///     Stop IISExpress process
        /// </summary>
        public override void Stop()
        {
            if (!IsRunning) return;

            _processRunner.Stop(_serverProcess);
            _serverProcess = null;
        }

        /// <summary>
        ///     Restart IISExpress process
        /// </summary>
        public override void Reset()
        {
            Stop();
            Start();
        }

        /// <summary>
        ///     Create a new web application and register it in the applicationhost.config 
        ///     file which is used by current IISExpress process
        /// </summary>
        /// <param name="webAppName"></param>
        /// <param name="forceUniqueName"></param>
        /// <returns></returns>
        public override WebApplicationInfo CreateWebApplication(string webAppName, bool forceUniqueName = true)
        {
            var appPhysicalPath = Path.Combine(RootPhysicalPath, webAppName);

            _iisExpressConfig.AddApplication(webAppName, appPhysicalPath);
            _iisExpressConfig.StoreSchema();

            var virtualPath = GetVirtualPath(webAppName);
            var remoteVirtualPath = virtualPath.Replace("localhost", Environment.MachineName);

            return new WebApplicationInfo
            {
                Name = webAppName,
                PhysicalPath = appPhysicalPath,
                VirtualPath = virtualPath,
                RemoteVirtualPath = remoteVirtualPath
            };
        }

        /// <summary>
        ///     Get details for a given web application, if it exists
        /// </summary>
        /// <param name="webAppName"></param>
        /// <returns></returns>
        public override WebApplicationInfo GetWebApplicationInfo(string webAppName)
        {
            var appPhysicalPath = _iisExpressConfig.GetApplicationProperty(webAppName, "physicalPath");

            if (!string.IsNullOrEmpty(appPhysicalPath))
            {
                var virtualPath = GetVirtualPath(webAppName);
                var remoteVirtualPath = virtualPath.Replace("localhost", Environment.MachineName);

                return new WebApplicationInfo
                {
                    Name = webAppName,
                    PhysicalPath = appPhysicalPath,
                    VirtualPath = virtualPath,
                    RemoteVirtualPath = remoteVirtualPath
                };
            }

            return null;
        }

        /// <summary>
        ///     Delete web application
        /// </summary>
        /// <param name="webAppName"></param>
        /// <param name="deletePhysicalFolder"></param>
        public override void DeleteWebApplication(string webAppName, bool deletePhysicalFolder = true)
        {
            var appInfo = GetWebApplicationInfo(webAppName);
            if (appInfo != null)
            {
                _iisExpressConfig.RemoveApplication(webAppName);
                _iisExpressConfig.StoreSchema();

                if (deletePhysicalFolder)
                {
                    _fileSystem.DirectoryDelete(appInfo.PhysicalPath, true);
                }
            }
        }

        private string LocateIisExpress()
        {
            var iisExpressPath = "";

            switch (_architecture)
            {
                case Architecture.Default:
                    iisExpressPath = _environmentSystem.ExpandEnvironmentVariables(PathToIssexpress);
                    if (!_fileSystem.FileExists(iisExpressPath))
                    {
                        iisExpressPath = _environmentSystem.ExpandEnvironmentVariables(PathToIssexpress.Replace("%PROGRAMFILES%", "%PROGRAMFILES(x86)%"));
                    }
                    break;
                case Architecture.x86:
                    if (_environmentSystem.Is64BitOperatingSystem)
                    {
                        iisExpressPath = _environmentSystem.ExpandEnvironmentVariables(PathToIssexpress.Replace("%PROGRAMFILES%", "%PROGRAMFILES(x86)%"));
                    }
                    else
                    {
                        iisExpressPath = _environmentSystem.ExpandEnvironmentVariables(PathToIssexpress);
                    }
                    break;
                case Architecture.Amd64:
                    iisExpressPath = _environmentSystem.ExpandEnvironmentVariables(PathToIssexpress);
                    break;
            }        

            if (string.IsNullOrEmpty(iisExpressPath) || !_fileSystem.FileExists(iisExpressPath))
            {
                throw new Exception("IISExpress was not found. Check if it was installed or architecture was specified correctly.");
            }

            _version = _fileSystem.FileGetVersion(iisExpressPath);

            return iisExpressPath;
        }

        private string CreateApplicationHostConfigFromTemplate()
        {
            var templatePath = Path.Combine(
                Path.GetDirectoryName(_serverExecutablePath) ?? "", @"config\templates\PersonalWebServer\applicationhost.config");

            if (!_fileSystem.FileExists(templatePath))
            {
                throw new Exception(string.Format("Template file for IIS Express does not exist at '{0}'.", templatePath));
            }

            var newApplicationHostConfigPath = Path.Combine(_rootPhysicalPath, "applicationhost.config");
            _fileSystem.FileCopy(templatePath, newApplicationHostConfigPath, true);

            return newApplicationHostConfigPath;
        }

        private void InitializeApplicationHostConfig()
        {
            _iisExpressConfig.AddApplicationPool(DefaultAppPoolName);
            _iisExpressConfig.SetDefaultApplicationPool(DefaultAppPoolName);
            _iisExpressConfig.AddBinding("http", string.Format("*:{0}:localhost", _httpPort));
            _iisExpressConfig.StoreSchema();
        }

        private string GetVirtualPath(string webAppName)
        {
            return string.Format("http://localhost:{0}/{1}", _httpPort, webAppName);
        }

        #region IIisCompatibleWebServer

        private ServerManager _serverManager;

        /// <summary>
        ///     Entry point to IISExpress metabase using Microsoft.Web.Administration APIs (works only if 
        ///     full IIS is installed on the machine)
        /// </summary>
        public ServerManager ServerManager
        {
            get
            {
                if (_serverManager == null)
                {
                    _serverManager = _serverManagerProvider.GetServerManager(_applicationHostConfigPath);
                }

                return _serverManager;
            }
        }

        #endregion
    }
}
