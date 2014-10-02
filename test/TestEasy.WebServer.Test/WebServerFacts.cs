using System;
using System.Collections.Generic;
using TestEasy.Core;
using TestEasy.Core.Configuration;
using Xunit;

namespace TestEasy.WebServer.Test
{
    public class WebServerFacts
    {
        public class Create
        {
            private WebServerMockGenerator _mock;
            private Dependencies _dependencies;

            public Create()
            {
                TestEasyConfig.Instance = new TestEasyConfig((object)null);

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
            public void WhenCreate_IfTypeIsUnknown_ShouldThrow()
            {
                // Arrange  
                // Act
                var exception = Assert.Throws<NotSupportedException>(
                    () => WebServer.Create("UnknownType", "", new WebServerSettings(), _dependencies));

                // Assert 
                Assert.Equal(string.Format("WebServer type is unsupported '{0}', type info: '{1}'. Make sure your web server type is registered in webServerTypes collection in default.config or testsuite.config.",
                    "UnknownType", ""), exception.Message);
            }

            [Fact]
            public void WhenCreate_IfTypeInfoIsEmpty_ShouldThrow()
            {
                // Arrange  
                _mock.MockWebServerRegisteredButTypeInfoIsEmpty();

                // Act
                var exception = Assert.Throws<NotSupportedException>(
                    () => WebServer.Create("UnknownType", "", new WebServerSettings(), _dependencies));

                // Assert 
                Assert.Equal(string.Format("WebServer type is unsupported '{0}', type info: '{1}'. Make sure your web server type is registered in webServerTypes collection in default.config or testsuite.config.",
                    "UnknownType", ""), exception.Message);
            }

            [Fact]
            public void WhenCreate_IfIisNotRegistered_ShouldUseItAutomatically()
            {
                // Arrange  
                _mock.MockWebServerIisInConfig()
                    .MockWebServerInstantiateFromCurrentAssembly();

                // Act
                var exception = Assert.Throws<NotSupportedException>(
                    () => WebServer.Create("IIS", "", new WebServerSettings(), _dependencies));

                // Assert 
                Assert.Equal(string.Format("WebServer type is unsupported '{0}', type info: '{1}'. Make sure your web server type is registered in webServerTypes collection in default.config or testsuite.config.",
                    "IIS", "TestEasy.WebServer.WebServerIis"), exception.Message);
            }

            [Fact]
            public void WhenCreate_IfIisExpressNotRegistered_ShouldUseItAutomatically()
            {
                // Arrange  
                _mock.MockWebServerIisInConfig()
                    .MockWebServerInstantiateFromCurrentAssembly();

                // Act
                var exception = Assert.Throws<NotSupportedException>(
                    () => WebServer.Create("IISExpress", "", new WebServerSettings(), _dependencies));

                // Assert 
                Assert.Equal(string.Format("WebServer type is unsupported '{0}', type info: '{1}'. Make sure your web server type is registered in webServerTypes collection in default.config or testsuite.config.",
                    "IISExpress", "TestEasy.WebServer.WebServerIisExpress"), exception.Message);
            }

            [Fact]
            public void WhenCreate_IfCustomWebserver_ShouldCreate()
            {
                // Arrange  
                _mock.MockWebServer()
                     .MockWebServerInstantiateFromCurrentAssembly();

                // Act
                var server = WebServer.Create(_mock.MockWebServerName, "", null, _dependencies);

                // Assert 
                Assert.Equal(_mock.MockWebServerName, server.Type);
            }

            [Fact]
            public void WhenCreate_IfWebServerFromCustomAssembly_ShouldCreate()
            {
                // Arrange  
                _mock.MockWebServerFromCustomAssembly()
                    .MockWebServerInstantiateFromCustomAssembly();

                // Act
                var exception = Assert.Throws<NotSupportedException>(
                    () => WebServer.Create(_mock.MockWebServerName, "", new WebServerSettings(), _dependencies));

                // Assert 
                Assert.Equal(string.Format("WebServer type is unsupported '{0}', type info: '{1}'. Make sure your web server type is registered in webServerTypes collection in default.config or testsuite.config.",
                    _mock.MockWebServerName, _mock.MockWebServerType + ", " + _mock.MockCustomAssemblyName), exception.Message);
            }

            [Fact]
            public void WhenCreate_IfCustomWebserverAndWebSiteProvided_ShouldCreate()
            {
                // Arrange  
                _mock.MockWebServer()
                    .MockWebServerInstantiateFromCurrentAssembly()
                    .MockWebServerCreateWithWebSiteProvided()
                    .MockWebServerCreate_RemoteFalse();
                TestEasyHelpers.Tools = _mock.ToolsHelper.Object;

                // Act
                var settings = new WebServerSettings();
                var server = WebServer.Create(_mock.MockWebServerName, _mock.MockCreateWebSiteName, settings, _dependencies);

                // Assert 
                Assert.Equal(_mock.MockWebServerName, server.Type);
                Assert.Equal(_mock.MockCreateWebSiteName, settings.WebAppName);
                Assert.Equal("localhost", settings.HostName);
                Assert.Equal(100000, settings.StartupTimeout);
            }

            [Fact]
            public void WhenCreate_IfCustomWebserverAndWebSiteProvidedAndRemoteTrue_ShouldCreate()
            {
                // Arrange  
                _mock.MockWebServer()
                    .MockWebServerInstantiateFromCurrentAssembly()
                    .MockWebServerCreateWithWebSiteProvidedRemoteTrue()
                    .MockWebServerCreate_RemoteTrue();
                    
                TestEasyHelpers.Tools = _mock.ToolsHelper.Object;

                // Act
                var settings = new WebServerSettings();
                var server = WebServer.Create(_mock.MockWebServerName, _mock.MockCreateWebSiteName, settings, _dependencies);

                // Assert 
                Assert.Equal(_mock.MockWebServerName, server.Type);
                Assert.Equal(_mock.MockCreateWebSiteName, settings.WebAppName);
                Assert.Equal(_mock.MockMachineName, settings.HostName);
                Assert.Equal(100000, settings.StartupTimeout);
            }
        }

        public class DeployWebApplication
        {
            private WebServerMockGenerator _mock;
            private Dependencies _dependencies;

            public DeployWebApplication()
            {
                TestEasyConfig.Instance = new TestEasyConfig((object)null);
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
            public void WhenBuildWebApplication_IfWebAppNotFound_ShouldThrow()
            {
                // Arrange  
                var server = new MockWebServer(new WebServerSettings(), _dependencies);

                // Act
                var exception = Assert.Throws<Exception>(
                    () => server.DeployWebApplication("someapp", null));

                // Assert 
                Assert.Equal(string.Format("Web Application not found on web server: '{0}'", "someapp"), exception.Message);
            }

            [Fact]
            public void WhenBuildWebApplication_IfWebAppFound_ShouldDeploy()
            {
                // Arrange  
                _mock.MockWebServerDeployWebApp();
                var server = new MockWebServer(new WebServerSettings(), _dependencies);
                server.WebApplicationInfo = _mock.MockWebApplicationInfo;

                // Act
                server.DeployWebApplication(_mock.MockCreateWebSiteName, _mock.MockDeploymentList);
            }
        }

        public class BuildWebApplication
        {
            private WebServerMockGenerator _mock;

            public BuildWebApplication()
            {
                TestEasyConfig.Instance = new TestEasyConfig((object)null);
                _mock = new WebServerMockGenerator();
            }

            [Fact]
            public void WhenBuildWebApplication_IfProjectNameIsEmpty_ShouldUseAppName()
            {
                // Arrange  
                _mock.MockBuildHelperNoProjectName();
                TestEasyHelpers.Builder = _mock.BuildHelper.Object;

                var server = new MockWebServer
                {
                    WebApplicationInfo = new WebApplicationInfo
                    {
                        PhysicalPath = _mock.MockUniqueTempPath,
                        Name = _mock.MockCreateWebSiteName
                    }
                };

                // Act
                server.BuildWebApplication(_mock.MockCreateWebSiteName);
            }

            [Fact]
            public void WhenBuildWebApplication_IfProjectNameIsNotEmpty_ShouldUseProjectName()
            {
                // Arrange  
                _mock.MockBuildHelperWithProjectName();
                TestEasyHelpers.Builder = _mock.BuildHelper.Object;

                var server = new MockWebServer
                {
                    WebApplicationInfo = new WebApplicationInfo
                    {
                        PhysicalPath = _mock.MockUniqueTempPath,
                        Name = _mock.MockCreateWebSiteName
                    }
                };

                // Act
                server.BuildWebApplication(_mock.MockCreateWebSiteName, _mock.MockProjectName);
            }    
        }

        public class SetCustomHeaders
        {
            private WebServerMockGenerator _mock;
            private Dependencies _dependencies;

            public SetCustomHeaders()
            {
                TestEasyConfig.Instance = new TestEasyConfig((object)null);
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
            public void WhenSetCustomHeaders_IfNoWebConfig_ShouldCreateIt()
            {
                // Arrange  
                _mock.MockSetCustomHeadersNoWebConfig();

                var server = new MockWebServer(new WebServerSettings(), _dependencies);
                
                // Act
                server.SetCustomHeaders(_mock.MockWebConfigPath, new Dictionary<string, string>
                {
                    { "header1", "value1" },
                    { "header2", "value2" }
                });
            }

            [Fact]
            public void WhenSetCustomHeaders_IfWebConfigExists_ShouldOverwrite()
            {
                // Arrange  
                _mock.MockSetCustomHeadersWithWebConfig();

                var server = new MockWebServer(new WebServerSettings(), _dependencies);

                // Act
                server.SetCustomHeaders(_mock.MockWebConfigPath, new Dictionary<string, string>
                {
                    { "header1", "value1" },
                    { "header2", "value2" }
                });
            } 
        }        
    }
}
