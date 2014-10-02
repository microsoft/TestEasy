using System;
using Xunit;
using Xunit.Extensions;

namespace TestEasy.WebServer.Test
{
    public class WebServerIisFacts
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
            public void WhenIis_IfInetInfoFound_ShouldTakeItsVersion()
            {
                // Arrange  
                _mock.MockWebServerIisCtor_InetInfoFileExists();

                // Act
                var server = new WebServerIis(new WebServerSettings(), _dependencies, false);

                // Assert 
                Assert.Equal("11.0", server.Version.ToString(2));
                Assert.Equal("IIS", server.Type);
            }

            [Theory]
            [InlineData("6.3", "8.5")]
            [InlineData("6.2", "8.0")]
            [InlineData("6.1", "7.5")]
            [InlineData("6.0", "7.0")]
            public void WhenIis_IfInetInfoDoesNotExist_ShouldTakeOsVersion(string osVersion, string expectedIisVersion)
            {
                // Arrange  
                _mock.MockWebServerIisCtor_InetInfoFileDoesNotExist_OSVersion(osVersion);

                // Act
                var server = new WebServerIis(new WebServerSettings(), _dependencies, false);

                // Assert 
                Assert.Equal(expectedIisVersion, server.Version.ToString(2));
                Assert.Equal("IIS", server.Type);
            }

            [Fact]
            public void WhenIis_IfInetInfoDoesNotExistAndOsVersionUnknown_ShouldThrow()
            {
                // Arrange  
                _mock.MockWebServerIisCtor_InetInfoFileDoesNotExist_OSVersionUnknown();

                // Act
                // Assert 
                var exception = Assert.Throws<Exception>(
                    () => new WebServerIis(new WebServerSettings(), _dependencies, false));

                Assert.Equal(@"Unable to determine version of IIS. Check if IIS is installed and if tests are executed as a process with native OS architecture.", exception.Message);
            }

            [Fact]
            public void WhenIis_IfRemoteTrue_ShouldUseMachineNameAsHost()
            {
                // Arrange  
                _mock.MockWebServerIisCtor_InetInfoFileExists()
                     .MockWebServerCreateWithWebSiteProvidedRemoteTrue();

                // Act
                var server = new WebServerIis(new WebServerSettings(), _dependencies, true);

                // Assert 
                Assert.Equal("IIS", server.Type);
                Assert.Equal(_mock.MockMachineName, server.HostName);
                Assert.Equal(_mock.MockWwwRootPath, server.RootPhysicalPath);
                Assert.Equal(_mock.MockApplicationHostConfig, server.Configs["applicationhost.config"]);
            }

            [Fact]
            public void WhenIis_IfRemoteFalse_ShouldUseGivenHost()
            {
                // Arrange  
                _mock.MockWebServerIisCtor_InetInfoFileExists()
                     .MockWebServerCreate_RemoteFalse();

                // Act
                var server = new WebServerIis(new WebServerSettings { HostName = "MyHost" }, _dependencies, false);

                // Assert 
                Assert.Equal("IIS", server.Type);
                Assert.Equal("MyHost", server.HostName);
                Assert.Equal(_mock.MockWwwRootPath, server.RootPhysicalPath);
                Assert.Equal(_mock.MockApplicationHostConfig, server.Configs["applicationhost.config"]);
            }
        }
    }
}
