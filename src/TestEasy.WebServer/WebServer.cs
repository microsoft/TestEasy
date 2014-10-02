using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TestEasy.Core;
using TestEasy.Core.Abstractions;
using TestEasy.Core.Configuration;

namespace TestEasy.WebServer
{
    /// <summary>
    ///     Base class for all web server wrappers. Contains static factory methods that
    ///     read <webServer> section in default.config or testsuite.config to see all 
    ///     available web server wrappers.
    /// </summary>
    public abstract class WebServer : IDisposable
    {
        /// <summary>
        ///     Create a web server given web server's type
        /// </summary>
        /// <param name="webServerType"></param>
        /// <param name="websiteName"></param>
        /// <returns></returns>
        public static WebServer Create(string webServerType = "", string websiteName = "")
        {
            return Create(webServerType, websiteName, null, Dependencies.Instance);
        }

        /// <summary>
        ///     Create a web server given web server's type
        /// </summary>
        /// <param name="webServerType"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static WebServer Create(string webServerType, WebServerSettings settings)
        {
            return Create(webServerType, "", settings, Dependencies.Instance);
        }

        /// <summary>
        ///     Create a web server given web server's type
        /// </summary>
        /// <param name="webServerType"></param>
        /// <param name="websiteName"></param>
        /// <param name="settings"></param>
        /// <param name="dependencies"></param>
        /// <returns></returns>
        internal static WebServer Create(
            string webServerType, 
            string websiteName,
            WebServerSettings settings, 
            Dependencies dependencies)
        {
            var type = string.IsNullOrEmpty(webServerType) ? TestEasyConfig.Instance.WebServer.Type : webServerType;

            if (settings == null)
            {
                settings = new WebServerSettings();
            }

            if (!string.IsNullOrEmpty(websiteName))
            {
                settings.HostName = TestEasyConfig.Instance.Client.Remote ? dependencies.EnvironmentSystem.MachineName : "localhost";
                settings.WebAppName = websiteName;
                settings.StartupTimeout = TestEasyConfig.Instance.WebServer.StartupTimeout;             
            }

            var typeInfo = (TestEasyConfig.Instance.WebServer.Types[type.ToLowerInvariant()] == null)
                ? ""
                : TestEasyConfig.Instance.WebServer.Types[type.ToLowerInvariant()].Type;
            if (string.IsNullOrEmpty(typeInfo))
            {
                if (type.Equals("IIS", StringComparison.InvariantCultureIgnoreCase))
                {
                    typeInfo = "TestEasy.WebServer.WebServerIis";
                }
                else if (type.Equals("IISExpress", StringComparison.InvariantCultureIgnoreCase))
                {
                    typeInfo = "TestEasy.WebServer.WebServerIisExpress";
                }
            }

            WebServer server = null;
            if (!string.IsNullOrEmpty(typeInfo))
            {
                server = Instantiate(typeInfo, settings, dependencies);
            }

            if (server == null)
            {
                throw new NotSupportedException(string.Format("WebServer type is unsupported '{0}', type info: '{1}'. Make sure your web server type is registered in webServerTypes collection in default.config or testsuite.config.",
                    type, typeInfo));
            }

            return server;
        }

        private static WebServer Instantiate(string typeInfo, WebServerSettings settings, Dependencies dependencies)
        {
            var assembly = dependencies.FileSystem.GetCallingAssembly();
            var typeName = "";

            if (!string.IsNullOrEmpty(typeInfo))
            {
                string[] typeTokens = typeInfo.Split(',');

                // if assembly name is specified and is not not current assembly
                if (typeTokens.Length == 2
                    && (assembly == null || string.Compare(typeTokens[1].Trim(), assembly.GetName().Name, StringComparison.InvariantCultureIgnoreCase) != 0))
                {
                    var assemblyName = typeTokens[1].Trim();                    
                    var assemblyPath = dependencies.FileSystem.GetExecutingAssemblyDirectory();
                    var dllPath = Path.Combine(assemblyPath, assemblyName + ".dll");
                    var exePath = Path.Combine(assemblyPath, assemblyName + ".dll");
                    if (dependencies.FileSystem.FileExists(dllPath))
                    {
                        assemblyName = dllPath;
                    }
                    else if (dependencies.FileSystem.FileExists(exePath))
                    {
                        assemblyName = exePath;
                    }

                    assembly = dependencies.FileSystem.LoadAssemblyFromFile(assemblyName);
                }

                typeName = typeTokens[0].Trim();
            }

            if (assembly == null)
            {
                return null;
            }

            var type = assembly.GetType(typeName);

            if (type == null)
            {
                return null;
            }

            return (WebServer)Activator.CreateInstance(type, settings, dependencies);            
        }

        public const string DefaultAppPoolName = "DefaultAppPool";
        public const string DefaultWebsiteName = "Default Web Site";

// ReSharper disable InconsistentNaming
        protected IFileSystem _fileSystem;
        protected IEnvironmentSystem _environmentSystem;
        protected IProcessRunner _processRunner;
        protected IServerManagerProvider _serverManagerProvider;
        protected string _type;
        protected string _hostName;
        protected string _rootPhysicalPath;
        protected Version _version;
        protected IDictionary<string, string> _configs = new Dictionary<string, string>();
// ReSharper restore InconsistentNaming

        /// <summary>
        ///     Web server type
        /// </summary>
        public string Type
        {
            get { return _type; }
        }

        /// <summary>
        ///     Web server version
        /// </summary>
        public Version Version
        {
            get { return _version; }
        }

        /// <summary>
        ///     Hostname used by web server to serve requests
        /// </summary>
        public string HostName
        {
            get { return _hostName; }
        }

        /// <summary>
        ///     Path to web server's physical root folder
        /// </summary>
        public string RootPhysicalPath
        {
            get { return _rootPhysicalPath; }
        }

        /// <summary>
        ///     List of config files and their locations on the test machine (specific to server type)
        /// </summary>
        public IDictionary<string, string> Configs
        {
            get { return _configs; }
        }

        /// <summary>
        ///     Start web server process
        /// </summary>
        public virtual void Start()
        {
        }

        /// <summary>
        ///     Stop web server process
        /// </summary>
        public virtual void Stop()
        {
        }

        /// <summary>
        ///     Restart web server process
        /// </summary>
        public virtual void Reset()
        {
        }

        /// <summary>
        ///     Create a web application on web server
        /// </summary>
        /// <param name="webAppName"></param>
        /// <param name="forceUniqueName"></param>
        /// <returns></returns>
        public abstract WebApplicationInfo CreateWebApplication(string webAppName, bool forceUniqueName = true);

        /// <summary>
        ///     Get web application's properties
        /// </summary>
        /// <param name="webAppName"></param>
        /// <returns></returns>
        public abstract WebApplicationInfo GetWebApplicationInfo(string webAppName);

        /// <summary>
        ///     Remove web appication
        /// </summary>
        /// <param name="webAppName"></param>
        /// <param name="deletePhysicalFolder"></param>
        public abstract void DeleteWebApplication(string webAppName, bool deletePhysicalFolder = true);

        /// <summary>
        ///     Create web application
        /// </summary>
        /// <param name="webAppName"></param>
        /// <param name="deploymentItems"></param>
        /// <param name="forceUniqueName"></param>
        /// <returns></returns>
        public WebApplicationInfo CreateWebApplication(string webAppName, IEnumerable<DeploymentItem> deploymentItems, bool forceUniqueName = true)
        {
            var appInfo = CreateWebApplication(webAppName, forceUniqueName);
            DeployWebApplication(webAppName, deploymentItems);
            return appInfo;
        }

        /// <summary>
        ///     Deploy file, folder or file content to a web application on server
        /// </summary>
        /// <param name="webAppName"></param>
        /// <param name="deploymentItems"></param>
        public virtual void DeployWebApplication(string webAppName, IEnumerable<DeploymentItem> deploymentItems)
        {
            var appInfo = GetWebApplicationInfo(webAppName);

            if (appInfo == null)
            {
                throw new Exception(string.Format("Web Application not found on web server: '{0}'", webAppName));
            }

            foreach (var di in deploymentItems)
            {
                var targetPath = string.IsNullOrEmpty(di.TargetRelativePath)
                    ? appInfo.PhysicalPath
                    : Path.Combine(appInfo.PhysicalPath, di.TargetRelativePath);

                if (!_fileSystem.DirectoryExists(targetPath))
                {
                    _fileSystem.DirectoryCreate(targetPath);
                }

                var fileOrDirName = Path.GetFileName(di.Path) ?? di.Path;
                var currentTargetPath = Path.Combine(targetPath, fileOrDirName);
                switch (di.Type)
                {
                    case DeploymentItemType.File:
                        _fileSystem.FileCopy(di.Path, currentTargetPath, true);
                        break;
                    case DeploymentItemType.Directory:
                        _fileSystem.DirectoryCopy(di.Path, targetPath);
                        break;
                    case DeploymentItemType.Content:
                        _fileSystem.FileWrite(currentTargetPath, di.Content);
                        break;
                }
            }
        }

        /// <summary>
        ///     Build web application (WAP + csproj) using msbuild.exe
        /// </summary>
        /// <param name="webAppName"></param>
        /// <param name="projectName"></param>
        public void BuildWebApplication(string webAppName, string projectName = "")
        {
            if (string.IsNullOrEmpty(projectName))
            {
                projectName = webAppName;
            }

            var appInfo = GetWebApplicationInfo(webAppName);
            TestEasyHelpers.Builder.Build(appInfo.PhysicalPath, projectName);
        }

        /// <summary>
        ///     Add custom headers to web server's root web.config
        /// </summary>
        /// <param name="webConfigPath"></param>
        /// <param name="headers"></param>
        public void SetCustomHeaders(string webConfigPath, IDictionary<string, string> headers)
        {
            const string overrideTemplate = @"<configuration><system.webServer><httpProtocol><customHeaders>{0}</customHeaders></httpProtocol></system.webServer></configuration>";

            var innerXml = new StringBuilder("<clear />");
            const string template = @"<add name=""{0}"" value=""{1}"" />";
            foreach (var kv in headers)
            {
                innerXml.Append(string.Format(template, kv.Key, kv.Value));
            }

            var sourceConfig = "";            
            if (_fileSystem.FileExists(webConfigPath))
            {
                sourceConfig = _fileSystem.FileReadAllText(webConfigPath);
            }

            var overrideConfig = string.Format(overrideTemplate, innerXml);

            var t = TestEasyHelpers.Xml.MergeXml(sourceConfig, overrideConfig);
            _fileSystem.FileWrite(webConfigPath, TestEasyHelpers.Xml.MergeXml(sourceConfig, overrideConfig));
        }

        #region IDisposable

        public void Dispose()
        {
            DisposeWebServer(true);

            GC.SuppressFinalize(this);
        }

        #endregion

        ~WebServer()
        {
            DisposeWebServer(false);
        }

        /// <summary>
        ///     Clean up resources
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void DisposeWebServer(bool disposing)
        {
            // clean up resources if any
        }
    }
}
