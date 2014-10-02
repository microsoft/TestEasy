using System;
using Xunit;

namespace TestEasy.WebServer.Test
{
    public class SiteExtensionsFacts
    {
        public class GetUniqueApplicaionName
        {
            [Fact]
            public void WhenUniqueAppName_ShouldJustReturn()
            {
                // Arrange  
                var mock = new WebServerMockGenerator();
                var uniqueAppName = mock.MockUniqueApplicationName;
                
                // Act
                var appName = mock.ServerManager.Sites[0].GetUniqueApplicaionName(uniqueAppName);

                // Assert 
                Assert.Equal(uniqueAppName, appName);
            }

            [Fact]
            public void WhenAppNameExist_ShouldIncrement()
            {
                // Arrange  
                var mock = new WebServerMockGenerator().
                    MockApplication();

                // Act
                var appName = mock.ServerManager.Sites[0].GetUniqueApplicaionName(mock.MockApplicationName);

                // Assert 
                Assert.Equal(mock.MockApplicationName.Trim('/') + "_1", appName);
            }
        }

        public class GetVirtualPath
        {
            [Fact]
            public void WhenHostNotProvided_ShouldReturnLocalHost()
            {
                // Arrange  
                var mock = new WebServerMockGenerator();

                // Act
                var path = SiteExtensions.GetVirtualPath(mock.ServerManager.Sites[0], "http", "");

                // Assert 
                Assert.Equal("http://localhost:8080", path);
            }

            [Fact]
            public void WhenHostNotProvided_IfPortNotProvided_ShouldReturnHostFromBindingAndPort80()
            {
                // Arrange  
                var mock = new WebServerMockGenerator();

                // Act
                var path = SiteExtensions.GetVirtualPath(mock.ServerManager.Sites[0], "https", "");

                // Assert 
                Assert.Equal("https://mylocalhost", path);
            }

            [Fact]
            public void WhenHostProvided_IfPortNotProvided_ShouldReturnHost80()
            {
                // Arrange  
                var mock = new WebServerMockGenerator();

                // Act
                var path = SiteExtensions.GetVirtualPath(mock.ServerManager.Sites[0], "https", "myhost");

                // Assert 
                Assert.Equal("https://myhost", path);
            }

            [Fact]
            public void WhenHostProvided_IfPortProvided_ShouldReturnHostAndPort()
            {
                // Arrange  
                var mock = new WebServerMockGenerator();

                // Act
                var path = SiteExtensions.GetVirtualPath(mock.ServerManager.Sites[0], "http", "myhost");

                // Assert 
                Assert.Equal("http://myhost:8080", path);
            }

            [Fact]
            public void WhenNoBindings_ShouldThrow()
            {
                // Arrange  
                var mock = new WebServerMockGenerator();
                mock.ServerManager.Sites[0].Bindings.Clear();

                // Act
                var exception = Assert.Throws<Exception>(() => SiteExtensions.GetVirtualPath(mock.ServerManager.Sites[0], "http", "myhost"));

                // Assert 
                Assert.Equal(string.Format("Binding for protocol 'http' is not defined for the website '{0}'.", mock.ServerManager.Sites[0].Name), exception.Message);
            }            
        }
    }
}
