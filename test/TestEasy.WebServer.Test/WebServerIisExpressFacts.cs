using System;
using System.IO;
using TestEasy.Core;
using Xunit;

namespace TestEasy.WebServer.Test
{
    public class WebServerIisExpressFacts
    {
        public class Constructor
        {
            private WebServerMockGenerator _mock;
            private Dependencies _dependencies;

            public Constructor()
            {
                _mock = new WebServerMockGenerator();
                _dependencies = new Dependencies
                {
                    FileSystem = _mock.FileSystem.Object,
                    EnvironmentSystem = _mock.EnvironmentSystem.Object,
                    ProcessRunner = _mock.ProcessRunner.Object,
                    ServerManagerProvider = _mock.ServerManagerProvider.Object
                };
            }

            [Fact]
            public void WhenIisExpress_IfRemote_ShouldThrow()
            {
                // Arrange  
                _mock.MockWebServerCreate_RemoteTrue();

                // Act
                var exception = Assert.Throws<NotSupportedException>(
                    () => new WebServerIisExpress(new WebServerSettings(), _dependencies));

                // Assert 
                Assert.Equal(@"For tests using remote browsers, IISExpress web server type is not supported, please use IIS.", exception.Message);
            }

            [Fact]
            public void WhenIisExpressAndRemoteFalse_IfLocateIisExpressAndArchitectureDefault_ShouldUseProgramFiles()
            {
                // Arrange  
                _mock.MockIisExpressRootPathInCurrentDir()
                    .MockWebServerIisExpressCtor_GetNextAvailablePort()
                    .MockWebServerIisExpressCtor_LocateIisExpress_ProgramFiles()
                    .MockWebServerIisExpressCtor_CreateApplicationHostConfigFromTemplate_Found(_mock.MockIisExpressPath)
                    .MockIisExpressConfig_ApplicationConfigFound(_mock.MockApplicationHostConfigPath)
                    .MockIisExpressConfig_RealDocumentInitialized(_mock.MockApplicationHostConfigPath)
                    .MockIisExpressConfig_StoreSchema(_mock.MockApplicationHostConfigPath)
                    .MockWebServerIisExpressStart(_mock.MockIisExpressPath)
                    .MockWebServerCreate_RemoteFalse();

                // Act
                var server = new WebServerIisExpress(new WebServerSettings(), _dependencies);

                // Assert 
                Assert.Equal("IISExpress", server.Type);
                Assert.Equal("localhost", server.HostName);
                Assert.Equal(Path.GetDirectoryName(_mock.MockCurrentExecutableDir), server.RootPhysicalPath);
                Assert.Equal(_mock.MockApplicationHostConfigPath, server.Configs["applicationhost.config"]);
                Assert.Equal("11.0", server.Version.ToString(2));

                _mock.MockWebServerIisExpressStop(_mock.MockIisExpressPath);
                server.Stop();

                _mock.MockIisExpressConfig_StoreSchema(_mock.MockApplicationHostConfigPath);
                var appInfo = server.CreateWebApplication("MyNewApp");

                Assert.Equal("MyNewApp", appInfo.Name);
                Assert.Equal(@"c:\currentdir\MyNewApp", appInfo.PhysicalPath);
                Assert.Equal(string.Format("http://{0}:1000/MyNewApp", Environment.MachineName), appInfo.RemoteVirtualPath);
                Assert.Equal("http://localhost:1000/MyNewApp", appInfo.VirtualPath);

                var newAppInfo = server.GetWebApplicationInfo("MyNewApp");
                Assert.Equal("MyNewApp", newAppInfo.Name);
                Assert.Equal(@"c:\currentdir\MyNewApp", newAppInfo.PhysicalPath);
                Assert.Equal(string.Format("http://{0}:1000/MyNewApp", Environment.MachineName), newAppInfo.RemoteVirtualPath);
                Assert.Equal("http://localhost:1000/MyNewApp", newAppInfo.VirtualPath);

                _mock.MockWebServerIisExpress_DeleteApp(newAppInfo.PhysicalPath);
                server.DeleteWebApplication("MyNewApp");
                newAppInfo = server.GetWebApplicationInfo("MyNewApp");
                Assert.Null(newAppInfo);
            }

            [Fact]
            public void WhenIisExpressAndRemoteFalse_IfLocateIisExpressAndArchitectureDefault1_ShouldUseProgramFiles()
            {
                // Arrange  
                const string webSiteName = "MyWebSite";
                _mock.MockToolsHelperGetUniqueTempPath(webSiteName)
                    .MockIisExpressRootPathInCurrentDir()
                    .MockWebServerIisExpressCtor_GetNextAvailablePort()
                    .MockWebServerIisExpressCtor_LocateIisExpress_ProgramFiles()
                    .MockWebServerIisExpressCtor_CreateApplicationHostConfigFromTemplate_Found_UniqueAppPath(_mock.MockIisExpressPath)
                    .MockIisExpressConfig_ApplicationConfigFound(_mock.MockApplicationHostConfigPath)
                    .MockIisExpressConfig_RealDocumentInitialized(_mock.MockApplicationHostConfigPath)
                    .MockIisExpressConfig_StoreSchema(_mock.MockApplicationHostConfigPath)
                    .MockWebServerIisExpressStart(_mock.MockIisExpressPath)
                    .MockWebServerCreate_RemoteFalse();
                TestEasyHelpers.Tools = _mock.ToolsHelper.Object;

                // Act
                var server = new WebServerIisExpress(new WebServerSettings { WebAppName = webSiteName }, _dependencies);

                // Assert 
                Assert.Equal("IISExpress", server.Type);
                Assert.Equal("localhost", server.HostName);
                Assert.Equal(_mock.MockUniqueTempPath, server.RootPhysicalPath);
                Assert.Equal(_mock.MockApplicationHostConfigPath, server.Configs["applicationhost.config"]);
                Assert.Equal("11.0", server.Version.ToString(2));

                _mock.MockWebServerIisExpressStop(_mock.MockIisExpressPath);
                server.Stop();
            }

        }

        public class Create
        {


            //[Fact]
            //public void WhenTypeIisExpressAndRemoteFalse_IfLocateIisExpressAndArchitectureDefault_ShouldUseProgramFilesx86()
            //{
            //    // Arrange  
            //    const string webSiteName = "MyWebSite";
            //    var mock = new WebServerMockGenerator();

            //    mock.MockToolsHelperGetUniqueTempPath(webSiteName)
            //        .MockWebServerIisExpressCtor_GetNextAvailablePort()
            //        .MockWebServerIisExpressCtor_LocateIisExpress_ProgramFiles86()
            //        .MockWebServerIisExpressCtor_CreateApplicationHostConfigFromTemplate_Found(mock.MockIisExpressPath86)
            //        .MockWebServerIisExpress_InitializeApplicationHostConfig()
            //        .MockWebServerIisExpressStart(mock.MockIisExpressPath86);

            //    TestEasyConfig.Instance.WebServer.Type = "IISExpress";
            //    TestEasyConfig.Instance.WebServer.StartupTimeout = 5;
            //    TestEasyConfig.Instance.Client.Remote = false;
            //    TestEasyHelpers.Tools = mock.ToolsHelper.Object;

            //    // Act
            //    var server = WebServer.Create(
            //        webSiteName,
            //        mock.FileSystem.Object,
            //        mock.EnvironmentSystem.Object,
            //        mock.ProcessRunner.Object,
            //        mock.ServerManagerProvider.Object);

            //    // Assert 
            //    Assert.Equal(WebServerType.IISExpress, server.Type);
            //    Assert.Equal("localhost", server.HostName);
            //    Assert.Equal(mock.MockUniqueTempPath, server.RootPhysicalPath);

            //    mock.MockWebServerIisExpressStop(mock.MockIisExpressPath86);
            //    server.Stop();
            //}

            //[Fact]
            //public void WhenTypeIisExpressAndRemoteFalse_IfLocateIisExpressAndArchitectureDefaultProgramFile86AlsoNotFound_ShouldThrow()
            //{
            //    // Arrange  
            //    const string webSiteName = "MyWebSite";
            //    var mock = new WebServerMockGenerator();

            //    mock.MockToolsHelperGetUniqueTempPath(webSiteName)
            //        .MockWebServerIisExpressCtor_GetNextAvailablePort()
            //        .MockWebServerIisExpressCtor_LocateIisExpress_ProgramFiles86AlsoNotFound()
            //        .MockWebServerIisExpressCtor_CreateApplicationHostConfigFromTemplate_Found(mock.MockIisExpressPath86);

            //    TestEasyConfig.Instance.WebServer.Type = "IISExpress";
            //    TestEasyConfig.Instance.WebServer.StartupTimeout = 5;
            //    TestEasyConfig.Instance.Client.Remote = false;
            //    TestEasyHelpers.Tools = mock.ToolsHelper.Object;

            //    // Act
            //    var exception = Assert.Throws<Exception>(() => WebServer.Create(
            //        webSiteName,
            //        mock.FileSystem.Object,
            //        mock.EnvironmentSystem.Object,
            //        mock.ProcessRunner.Object,
            //        mock.ServerManagerProvider.Object));

            //    // Assert 
            //    Assert.Equal("IISExpress was not found. Check if it was installed or architecture was specified correctly.", exception.Message);
            //}

            //[Fact]
            //public void WhenTypeIisExpressAndRemoteFalse_IfLocateIisExpressAndArchitecturex64_ShouldUseProgramFiles()
            //{
            //    // Arrange  
            //    var mock = new WebServerMockGenerator();

            //    mock.MockWebServerIisExpressCtor_GetNextAvailablePort()
            //        .MockWebServerIisExpressCtor_LocateIisExpress_ProgramFiles()
            //        .MockWebServerIisExpressCtor_CreateApplicationHostConfigFromTemplate_Found(mock.MockIisExpressPath)
            //        .MockWebServerIisExpress_InitializeApplicationHostConfig()
            //        .MockWebServerIisExpressStart(mock.MockIisExpressPath);

            //    // Act
            //    var server = WebServer.Create(
            //        WebServerType.IISExpress,
            //        new WebServerSettings { Architecture = Architecture.Amd64, HostName = "localhost", StartupTimeout = 5, RootPhysicalPath = mock.MockUniqueTempPath },
            //        mock.FileSystem.Object,
            //        mock.EnvironmentSystem.Object,
            //        mock.ProcessRunner.Object,
            //        mock.ServerManagerProvider.Object);

            //    // Assert 
            //    Assert.Equal(WebServerType.IISExpress, server.Type);
            //    Assert.Equal("localhost", server.HostName);
            //    Assert.Equal(mock.MockUniqueTempPath, server.RootPhysicalPath);

            //    mock.MockWebServerIisExpressStop(mock.MockIisExpressPath);
            //    server.Stop();
            //}

            //[Fact]
            //public void WhenTypeIisExpressAndRemoteFalse_IfLocateIisExpressAndArchitecturex86AndIs64BitOperatingSystemTrue_ShouldUseProgramFilesx86()
            //{
            //    // Arrange  
            //    var mock = new WebServerMockGenerator();

            //    mock.MockWebServerIisExpressCtor_GetNextAvailablePort()
            //        .MockWebServerIisExpressCtor_LocateIisExpress_Architecturex86_Is64BitOperatingSystemTrue()
            //        .MockWebServerIisExpressCtor_CreateApplicationHostConfigFromTemplate_Found(mock.MockIisExpressPath86)
            //        .MockWebServerIisExpress_InitializeApplicationHostConfig()
            //        .MockWebServerIisExpressStart(mock.MockIisExpressPath86);

            //    // Act
            //    var server = WebServer.Create(
            //        WebServerType.IISExpress,
            //        new WebServerSettings { Architecture = Architecture.x86, HostName = "localhost", StartupTimeout = 5, RootPhysicalPath = mock.MockUniqueTempPath },
            //        mock.FileSystem.Object,
            //        mock.EnvironmentSystem.Object,
            //        mock.ProcessRunner.Object,
            //        mock.ServerManagerProvider.Object);

            //    // Assert 
            //    Assert.Equal(WebServerType.IISExpress, server.Type);
            //    Assert.Equal("localhost", server.HostName);
            //    Assert.Equal(mock.MockUniqueTempPath, server.RootPhysicalPath);

            //    mock.MockWebServerIisExpressStop(mock.MockIisExpressPath86);
            //    server.Stop();
            //}

            //[Fact]
            //public void WhenTypeIisExpressAndRemoteFalse_IfLocateIisExpressAndArchitecturex86AndIs64BitOperatingSystemFalse_ShouldUseProgramFiles()
            //{
            //    // Arrange  
            //    var mock = new WebServerMockGenerator();

            //    mock.MockWebServerIisExpressCtor_GetNextAvailablePort()
            //        .MockWebServerIisExpressCtor_LocateIisExpress_Architecturex86_Is64BitOperatingSystemFalse()
            //        .MockWebServerIisExpressCtor_CreateApplicationHostConfigFromTemplate_Found(mock.MockIisExpressPath)
            //        .MockWebServerIisExpress_InitializeApplicationHostConfig()
            //        .MockWebServerIisExpressStart(mock.MockIisExpressPath);

            //    // Act
            //    var server = WebServer.Create(
            //        WebServerType.IISExpress,
            //        new WebServerSettings { Architecture = Architecture.x86, HostName = "localhost", StartupTimeout = 5, RootPhysicalPath = mock.MockUniqueTempPath },
            //        mock.FileSystem.Object,
            //        mock.EnvironmentSystem.Object,
            //        mock.ProcessRunner.Object,
            //        mock.ServerManagerProvider.Object);

            //    // Assert 
            //    Assert.Equal(WebServerType.IISExpress, server.Type);
            //    Assert.Equal("localhost", server.HostName);
            //    Assert.Equal(mock.MockUniqueTempPath, server.RootPhysicalPath);

            //    mock.MockWebServerIisExpressStop(mock.MockIisExpressPath);
            //    server.Stop();
            //}

            //[Fact]
            //public void WhenTypeIisExpressAndRemoteFalse_IfCreateApplicationHostConfigFromTemplateAndTemlateNotFound_ShouldThrow()
            //{
            //    // Arrange  
            //    const string webSiteName = "MyWebSite";
            //    var mock = new WebServerMockGenerator();

            //    mock.MockToolsHelperGetUniqueTempPath(webSiteName)
            //        .MockWebServerIisExpressCtor_GetNextAvailablePort()
            //        .MockWebServerIisExpressCtor_LocateIisExpress_ProgramFiles()
            //        .MockWebServerIisExpressCtor_CreateApplicationHostConfigFromTemplate_NotFound(mock.MockIisExpressPath);

            //    TestEasyConfig.Instance.WebServer.Type = "IISExpress";
            //    TestEasyConfig.Instance.WebServer.StartupTimeout = 5;
            //    TestEasyConfig.Instance.Client.Remote = false;
            //    TestEasyHelpers.Tools = mock.ToolsHelper.Object;

            //    // Act
            //    var exception = Assert.Throws<Exception>(() => WebServer.Create(
            //        webSiteName,
            //        mock.FileSystem.Object,
            //        mock.EnvironmentSystem.Object,
            //        mock.ProcessRunner.Object,
            //        mock.ServerManagerProvider.Object));

            //    // Assert 
            //    Assert.Equal(string.Format("Template file for IIS Express does not exist at '{0}'.",
            //            Path.Combine(Path.GetDirectoryName(mock.MockIisExpressPath) ?? "", @"config\templates\PersonalWebServer\applicationhost.config")),
            //            exception.Message);
            //}

            //[Fact]
            //public void WhenTypeIisExpressAndRemoteFalse_IfStartProcessFailed_ShouldThrow()
            //{
            //    // Arrange  
            //    const string webSiteName = "MyWebSite";
            //    var mock = new WebServerMockGenerator();
            //    mock.MockToolsHelperGetUniqueTempPath(webSiteName)
            //    .MockWebServerIisExpressCtor_GetNextAvailablePort()
            //    .MockWebServerIisExpressCtor_LocateIisExpress_ProgramFiles()
            //    .MockWebServerIisExpressCtor_CreateApplicationHostConfigFromTemplate_Found(mock.MockIisExpressPath)
            //    .MockWebServerIisExpress_InitializeApplicationHostConfig()
            //    .MockWebServerIisExpressStartProcessFailed(mock.MockIisExpressPath);

            //    TestEasyConfig.Instance.WebServer.Type = "IISExpress";
            //    TestEasyConfig.Instance.WebServer.StartupTimeout = 5;
            //    TestEasyConfig.Instance.Client.Remote = false;
            //    TestEasyHelpers.Tools = mock.ToolsHelper.Object;

            //    // Act
            //    var exception = Assert.Throws<Exception>(() => WebServer.Create(
            //        webSiteName,
            //        mock.FileSystem.Object,
            //        mock.EnvironmentSystem.Object,
            //        mock.ProcessRunner.Object,
            //        mock.ServerManagerProvider.Object));

            //    // Assert 
            //    Assert.Equal(string.Format("Failed to start process '{0} {1}'.", mock.MockIisExpressPath, mock.MockIssExpressProcessArguments),
            //            exception.Message);
            //}

            //[Fact]
            //public void WhenTypeIisExpressAndRemoteFalse_IfStartWaitForProcessFailed_ShouldThrow()
            //{
            //    // Arrange  
            //    const string webSiteName = "MyWebSite";
            //    var mock = new WebServerMockGenerator();
            //    mock.MockToolsHelperGetUniqueTempPath(webSiteName)
            //    .MockWebServerIisExpressCtor_GetNextAvailablePort()
            //    .MockWebServerIisExpressCtor_LocateIisExpress_ProgramFiles()
            //    .MockWebServerIisExpressCtor_CreateApplicationHostConfigFromTemplate_Found(mock.MockIisExpressPath)
            //    .MockWebServerIisExpress_InitializeApplicationHostConfig()
            //    .MockWebServerIisExpressStartWaitForProcessFailed(mock.MockIisExpressPath);

            //    TestEasyConfig.Instance.WebServer.Type = "IISExpress";
            //    TestEasyConfig.Instance.WebServer.StartupTimeout = 5;
            //    TestEasyConfig.Instance.Client.Remote = false;
            //    TestEasyHelpers.Tools = mock.ToolsHelper.Object;

            //    // Act
            //    var exception = Assert.Throws<Exception>(() => WebServer.Create(
            //        webSiteName,
            //        mock.FileSystem.Object,
            //        mock.EnvironmentSystem.Object,
            //        mock.ProcessRunner.Object,
            //        mock.ServerManagerProvider.Object));

            //    // Assert 
            //    Assert.Equal(string.Format("Process '{0} {1}' exited unexpctedly.", mock.MockIisExpressPath, mock.MockIssExpressProcessArguments),
            //            exception.Message);
            //}
        }
    } 
}
