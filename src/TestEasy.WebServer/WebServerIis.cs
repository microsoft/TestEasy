using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Web.Administration;
using TestEasy.Core.Configuration;

namespace TestEasy.WebServer
{
    /// <summary>
    ///     Provides helper API to manage web applications hosted in IIS web server. 
    ///     Uses and exposes Microsoft.Web.Administration APIs to manage metabase.
    /// </summary>
    public class WebServerIis : WebServer, IIisCompatibleWebServer
    {
        private readonly ServerManager _serverManager;

        /// <summary>
        ///     ctor
        /// </summary>
        public WebServerIis() 
            : this(new WebServerSettings(), Dependencies.Instance)
        {
        }

        /// <summary>
        ///     ctor
        /// </summary>
        /// <param name="settings"></param>
        public WebServerIis(WebServerSettings settings)
            : this(settings, Dependencies.Instance)
        {
        }

        /// <summary>
        ///     ctor
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="dependencies"></param>
        public WebServerIis(WebServerSettings settings, Dependencies dependencies)
            : this(settings, dependencies, TestEasyConfig.Instance.Client.Remote)
        {

        }

        /// <summary>
        ///     ctor
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="dependencies"></param>
        /// <param name="remote"></param>
        public WebServerIis(WebServerSettings settings, Dependencies dependencies, bool remote)
        {
            _environmentSystem = dependencies.EnvironmentSystem;
            _fileSystem = dependencies.FileSystem;
            _serverManagerProvider = dependencies.ServerManagerProvider;

            _type = "IIS";
            _hostName = remote
                ? settings.HostName.Replace("localhost", _environmentSystem.MachineName)
                : settings.HostName;
            _rootPhysicalPath = string.IsNullOrEmpty(settings.RootPhysicalPath)
                ? _environmentSystem.ExpandEnvironmentVariables(@"%SystemDrive%\inetpub\wwwroot")
                : settings.RootPhysicalPath;

            _version = GetIisVersion();
            _configs = new Dictionary<string, string>
            {
                {
                    "applicationhost.config",
                    _environmentSystem.ExpandEnvironmentVariables(
                        @"%SystemDrive%\Windows\System32\inetsrv\config\applicationHost.config")
                }
            };

            _serverManager = _serverManagerProvider.GetServerManager("");
        }

        /// <summary>
        ///     Create a web application in IIS metabase
        /// </summary>
        /// <param name="webAppName"></param>
        /// <param name="forceUniqueName"></param>
        /// <returns></returns>
        public override WebApplicationInfo CreateWebApplication(string webAppName, bool forceUniqueName = true)
        {
            var uniqueAppName = forceUniqueName ? _serverManager.Sites[DefaultWebsiteName].GetUniqueApplicaionName(webAppName) : webAppName;

            var appPhysicalPath = Path.Combine(RootPhysicalPath, uniqueAppName);
            var application = _serverManager.Sites[DefaultWebsiteName].Applications.Add("/" + uniqueAppName, appPhysicalPath);

            _serverManager.CommitChanges();

            return GetWebApplicationInfo(application);
        }

        /// <summary>
        ///     Get web application's properties
        /// </summary>
        /// <param name="webAppName"></param>
        /// <returns></returns>
        public override WebApplicationInfo GetWebApplicationInfo(string webAppName)
        {
            return GetWebApplicationInfo(_serverManager.Sites[DefaultWebsiteName].Applications["/" + webAppName]);
        }

        /// <summary>
        ///     Remove web application from IIS metabase
        /// </summary>
        /// <param name="webAppName"></param>
        /// <param name="deletePhysicalFolder"></param>
        public override void DeleteWebApplication(string webAppName, bool deletePhysicalFolder = true)
        {
            var application = _serverManager.Sites[DefaultWebsiteName].Applications["/" + webAppName];
            if (application != null)
            {
                var physicalPath = application.VirtualDirectories[0].PhysicalPath;
                application.Delete();
                _serverManager.CommitChanges();

                if (deletePhysicalFolder)
                {
                    _fileSystem.DirectoryDelete(physicalPath, true);   
                }
            }
        }

        private WebApplicationInfo GetWebApplicationInfo(Application application)
        {
            var virtualPath = _serverManager.Sites[DefaultWebsiteName].GetHttpVirtualPath() + application.Path;

            if (TestEasyConfig.Instance.Client.Remote)
            {
                virtualPath = virtualPath.Replace("localhost", Environment.MachineName);
            }

            var remoteVirtualPath = virtualPath.Replace("localhost", Environment.MachineName);
            return new WebApplicationInfo
            {
                Name = application.Path.Trim('/'),
                PhysicalPath = application.VirtualDirectories[0].PhysicalPath,
                VirtualPath = virtualPath,
                RemoteVirtualPath = remoteVirtualPath
            };
        }

        private Version GetIisVersion()
        {
            // Note: best way to determine version of IIS is check its file version, but if unit 
            // test using this assembly are executed as x86 process on x64 machine, file may not be
            // found. So we have a back up way to determine IIS version based on OS version.

            Version version;

            var exePath = _environmentSystem.ExpandEnvironmentVariables(@"%windir%\Sysnative\inetsrv\inetinfo.exe");

            if (_fileSystem.FileExists(exePath))
            {
                version = _fileSystem.FileGetVersion(exePath);
            }
            else
            {
                var osVersion = _environmentSystem.OSVersion.ToString(2);
                switch (osVersion)
                {
                    case "6.3":
                        version = new Version(8, 5);
                        break;
                    case "6.2":
                        version = new Version(8, 0);
                        break;
                    case "6.1":
                        version = new Version(7, 5);
                        break;
                    case "6.0":
                        version = new Version(7, 0);
                        break;
                    default:
                        throw new Exception(@"Unable to determine version of IIS. Check if IIS is installed and if tests are executed as a process with native OS architecture.");
                }
            }

            return version;
        }

        #region IIisCompatibleWebServer
        
        /// <summary>
        ///     Entry point to IISExpress metabase using Microsoft.Web.Administration APIs (works only if 
        ///     full IIS is installed on the machine)
        /// </summary>
        public ServerManager ServerManager
        {
            get { return _serverManager; }
        }

        #endregion
    }
}
