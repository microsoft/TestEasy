using System;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace TestEasy.WebServer.Test
{
    public class IisExpressConfigFacts
    {
        public class LoadSchema
        {
            private WebServerMockGenerator _mock;

            public LoadSchema()
            {
                _mock = new WebServerMockGenerator();
            }

            [Fact]
            public void WhenLoadSchema_IfApplicationHostConfigNotFound_ShouldThrow()
            {
                // Arrange 
                const string configPath = "unknownapp.config";
                _mock.MockIisExpressConfig_ApplicationConfigNotFound(configPath);
                // Act
                var exception = Assert.Throws<Exception>(
                    () => new IisExpressConfig(configPath, _mock.FileSystem.Object));

                // Assert 
                Assert.Equal(string.Format("ApplicationHost.config file was not found: '{0}'", configPath),
                    exception.Message);
            }

            [Fact]
            public void WhenLoadSchema_IfXDocumentNotLoaded_ShouldThrow()
            {
                // Arrange 
                const string configPath = "unknownapp.config";
                _mock.MockIisExpressConfig_ApplicationConfigFound(configPath)
                     .MockIisExpressConfig_DocumentNotLoaded(configPath);
                // Act
                var exception = Assert.Throws<Exception>(
                    () => new IisExpressConfig(configPath, _mock.FileSystem.Object));

                // Assert 
                Assert.Equal(string.Format("Somethng went wrong while loading xml schema from config file: '{0}'", configPath),
                    exception.Message);
            }

            [Fact]
            public void WhenLoadSchema_IfXDocumentHasNoSite_ShouldThrow()
            {
                // Arrange 
                const string configPath = "app.config";
                _mock.MockIisExpressConfig_ApplicationConfigFound(configPath)
                     .MockIisExpressConfig_DocumentLoaded(configPath);

                // Act
                var exception = Assert.Throws<Exception>(
                    () => new IisExpressConfig(configPath, _mock.FileSystem.Object));

                // Assert 
                Assert.Equal(string.Format("Site with id='{0}' was not found in '{1}'", "1", configPath),
                    exception.Message);
            }

            [Fact]
            public void WhenLoadSchema_IfXDocumentHasSite_ShouldLoad()
            {
                // Arrange 
                const string configPath = "app.config";
                _mock.MockIisExpressConfig_ApplicationConfigFound(configPath)
                    .MockIisExpressConfig_RealDocumentInitialized(configPath);

                // Act, Assert
                new IisExpressConfig(configPath, _mock.FileSystem.Object);
           }
        }

        public class AddApplication
        {
            private WebServerMockGenerator _mock;

            public AddApplication()
            {
                _mock = new WebServerMockGenerator();
            }
            
            [Fact]
            public void WhenAddApplication_IfAppDoesNotExist_ShouldAdd()
            {
                // Arrange 
                const string configPath = "app.config";
                _mock.MockIisExpressConfig_ApplicationConfigFound(configPath)
                    .MockIisExpressConfig_RealDocumentInitialized(configPath);

                // Act
                var config = new IisExpressConfig(configPath, _mock.FileSystem.Object);
                var app = config.AddApplication("MyApp", @"c:\somepath");

                //Assert
                XElement site = null;
                var xDefaultSites = config.Root.Document.Descendants("site").Where(s => s.Attribute("id").Value == "1").ToList();
                if (xDefaultSites.Any())
                {
                    site = xDefaultSites.First();
                }
                var xapplications = site.Descendants("application").ToList();

                Assert.True(xapplications.Any(a => a.Attribute("path").Value.Equals("/MyApp", StringComparison.InvariantCultureIgnoreCase)));
                Assert.Equal(@"c:\somepath", app.Descendants().First().Attribute("physicalPath").Value);
            }

            [Fact]
            public void WhenAddApplication_IfAppExist_ShouldOverwrite()
            {
                // Arrange 
                const string configPath = "app.config";
                _mock.MockIisExpressConfig_ApplicationConfigFound(configPath)
                    .MockIisExpressConfig_RealDocumentInitialized(configPath);

                // Act
                var config = new IisExpressConfig(configPath, _mock.FileSystem.Object);
                var app = config.AddApplication("", @"c:\somepath");

                //Assert
                XElement site = null;
                var xDefaultSites = config.Root.Document.Descendants("site").Where(s => s.Attribute("id").Value == "1").ToList();
                if (xDefaultSites.Any())
                {
                    site = xDefaultSites.First();
                }
                var xapplications = site.Descendants("application").ToList();

                Assert.True(xapplications.Any(a => a.Attribute("path").Value.Equals("/", StringComparison.InvariantCultureIgnoreCase)));
                Assert.Equal(@"c:\somepath", app.Descendants().First().Attribute("physicalPath").Value);
            }
        }

        public class GetApplicationProperty
        {
            private WebServerMockGenerator _mock;

            public GetApplicationProperty()
            {
                _mock = new WebServerMockGenerator();
            }

            [Fact]
            public void WhenGetApplicationProperty_IfAppNotFound_ShouldReturnNull()
            {
                // Arrange 
                const string configPath = "app.config";
                _mock.MockIisExpressConfig_ApplicationConfigFound(configPath)
                    .MockIisExpressConfig_RealDocumentInitialized(configPath);

                // Act
                var config = new IisExpressConfig(configPath, _mock.FileSystem.Object);
                var property = config.GetApplicationProperty("unknownapp", "unknownproperty");

                // Assert 
                Assert.Null(property);
            }

            [Fact]
            public void WhenGetApplicationProperty_IfAppExist_ShouldReturnProperty()
            {
                // Arrange 
                const string configPath = "app.config";
                _mock.MockIisExpressConfig_ApplicationConfigFound(configPath)
                    .MockIisExpressConfig_RealDocumentInitialized(configPath);

                // Act
                var config = new IisExpressConfig(configPath, _mock.FileSystem.Object);
                var property = config.GetApplicationProperty("", "physicalPath");

                // Assert 
                Assert.Equal(@"%IIS_SITES_HOME%\WebSite1", property);
            }
        }

        public class RemoveApplication
        {
            private WebServerMockGenerator _mock;

            public RemoveApplication()
            {
                _mock = new WebServerMockGenerator();
            }

            [Fact]
            public void WhenRemoveApplication_ShouldRemove()
            {
                // Arrange 
                const string configPath = "app.config";
                _mock.MockIisExpressConfig_ApplicationConfigFound(configPath)
                    .MockIisExpressConfig_RealDocumentInitialized(configPath);

                // Act
                var config = new IisExpressConfig(configPath, _mock.FileSystem.Object);
                config.RemoveApplication("");

                //Assert
                XElement site = null;
                var xDefaultSites = config.Root.Document.Descendants("site").Where(s => s.Attribute("id").Value == "1").ToList();
                if (xDefaultSites.Any())
                {
                    site = xDefaultSites.First();
                }
                var xapplications = site.Descendants("application").ToList();

                Assert.False(xapplications.Any(a => a.Attribute("path").Value.Equals("/", StringComparison.InvariantCultureIgnoreCase)));
            }
        }

        public class RemoveAllApplications
        {
            private WebServerMockGenerator _mock;

            public RemoveAllApplications()
            {
                _mock = new WebServerMockGenerator();
            }

            [Fact]
            public void WhenRemoveAllApplications_ShouldRemove()
            {
                // Arrange 
                const string configPath = "app.config";
                _mock.MockIisExpressConfig_ApplicationConfigFound(configPath)
                    .MockIisExpressConfig_RealDocumentInitialized(configPath);

                // Act
                var config = new IisExpressConfig(configPath, _mock.FileSystem.Object);
                config.RemoveAllApplications();

                //Assert
                XElement site = null;
                var xDefaultSites = config.Root.Document.Descendants("site").Where(s => s.Attribute("id").Value == "1").ToList();
                if (xDefaultSites.Any())
                {
                    site = xDefaultSites.First();
                }
                var xapplications = site.Descendants("application").ToList();

                Assert.False(xapplications.Any(a => a.Attribute("path").Value.Equals("/", StringComparison.InvariantCultureIgnoreCase)));
            }
        }

        public class AddBinding
        {
            private WebServerMockGenerator _mock;

            public AddBinding()
            {
                _mock = new WebServerMockGenerator();
            }

            [Fact]
            public void WhenAddBinding_IfAppDoesNotExist_ShouldAdd()
            {
                // Arrange 
                const string configPath = "app.config";
                _mock.MockIisExpressConfig_ApplicationConfigFound(configPath)
                    .MockIisExpressConfig_RealDocumentInitialized(configPath);

                var config = new IisExpressConfig(configPath, _mock.FileSystem.Object);
                XElement site = null;
                var xDefaultSites = config.Root.Document.Descendants("site").Where(s => s.Attribute("id").Value == "1").ToList();
                if (xDefaultSites.Any())
                {
                    site = xDefaultSites.First();
                }
                site.Descendants("bindings").Remove();
                
                // Act
                var binding = config.AddBinding("http", @"xxxx");

                //Assert
                Assert.True(site.Descendants("bindings").Any());
                Assert.Equal(@"xxxx", binding.Attribute("bindingInformation").Value);
            }

            [Fact]
            public void WhenAddBinding_IfAppExist_ShouldOverwrite()
            {
                // Arrange 
                const string configPath = "app.config";
                _mock.MockIisExpressConfig_ApplicationConfigFound(configPath)
                    .MockIisExpressConfig_RealDocumentInitialized(configPath);

                var config = new IisExpressConfig(configPath, _mock.FileSystem.Object);
                XElement site = null;
                var xDefaultSites = config.Root.Document.Descendants("site").Where(s => s.Attribute("id").Value == "1").ToList();
                if (xDefaultSites.Any())
                {
                    site = xDefaultSites.First();
                }

                // Act
                var binding = config.AddBinding("http", @"xxxx");

                //Assert
                Assert.True(site.Descendants("bindings").Any());
                Assert.Equal(@"xxxx", binding.Attribute("bindingInformation").Value);
            }
        }

        public class RemoveBinding
        {
            private WebServerMockGenerator _mock;

            public RemoveBinding()
            {
                _mock = new WebServerMockGenerator();
            }

            [Fact]
            public void WhenRemoveBinding_IfAppDoesNotExist_ShouldAdd()
            {
                // Arrange 
                const string configPath = "app.config";
                _mock.MockIisExpressConfig_ApplicationConfigFound(configPath)
                    .MockIisExpressConfig_RealDocumentInitialized(configPath);

                var config = new IisExpressConfig(configPath, _mock.FileSystem.Object);
                XElement site = null;
                var xDefaultSites = config.Root.Document.Descendants("site").Where(s => s.Attribute("id").Value == "1").ToList();
                if (xDefaultSites.Any())
                {
                    site = xDefaultSites.First();
                }
                var xbindings = site.Descendants("bindings").First();
                Assert.Equal(2, xbindings.Descendants().Count());

                // Act
                config.RemoveBinding("http");
                config.RemoveBinding("https");

                //Assert
                Assert.False(xbindings.Descendants().Any());
            }
        }

        public class AddApplicationPool
        {
            private WebServerMockGenerator _mock;

            public AddApplicationPool()
            {
                _mock = new WebServerMockGenerator();
            }

            [Fact]
            public void WhenAddApplicationPool_IfAppPoolDoesNotExist_ShouldAdd()
            {
                // Arrange 
                const string configPath = "app.config";
                _mock.MockIisExpressConfig_ApplicationConfigFound(configPath)
                    .MockIisExpressConfig_RealDocumentInitialized(configPath);

                var config = new IisExpressConfig(configPath, _mock.FileSystem.Object);
                config.Root.Document.Descendants("applicationPools").Remove();

                // Act
                var pool = config.AddApplicationPool("mypool", @"aaa", "mmm", "qqq");

                //Assert
                var xapppools = config.Root.Document.Descendants("applicationPools").ToList();
                var processModel = pool.Descendants().First();
                Assert.True(xapppools.Any());
                Assert.Equal(2, xapppools.Descendants().Count());
                Assert.Equal(@"mypool", pool.Attribute("name").Value);
                Assert.Equal(@"aaa", pool.Attribute("managedRuntimeVersion").Value);
                Assert.Equal(@"mmm", pool.Attribute("managedPipelineMode").Value);
                Assert.Equal(@"qqq", processModel.Attribute("identityType").Value);
            }

            [Fact]
            public void WhenAddApplicationPool_IfAppPoolExist_ShouldOverwrite()
            {
                // Arrange 
                const string configPath = "app.config";
                _mock.MockIisExpressConfig_ApplicationConfigFound(configPath)
                    .MockIisExpressConfig_RealDocumentInitialized(configPath);

                var config = new IisExpressConfig(configPath, _mock.FileSystem.Object);

                // Act
                var pool = config.AddApplicationPool("Clr4IntegratedAppPool", @"aaa", "mmm", "qqq");

                //Assert
                var xapppools = config.Root.Document.Descendants("applicationPools").ToList();
                var processModel = pool.Descendants().First();
                Assert.True(xapppools.Any());
                Assert.Equal(8, xapppools.Descendants().Count());
                Assert.Equal(@"Clr4IntegratedAppPool", pool.Attribute("name").Value);
                Assert.Equal(@"aaa", pool.Attribute("managedRuntimeVersion").Value);
                Assert.Equal(@"mmm", pool.Attribute("managedPipelineMode").Value);
                Assert.Equal(@"qqq", processModel.Attribute("identityType").Value);
            }
        }

        public class SetDefaultApplicationPool
        {
            private WebServerMockGenerator _mock;

            public SetDefaultApplicationPool()
            {
                _mock = new WebServerMockGenerator();
            }

            [Fact]
            public void WhenSetDefaultApplicationPool_ShouldSet()
            {
                // Arrange 
                const string configPath = "app.config";
                _mock.MockIisExpressConfig_ApplicationConfigFound(configPath)
                    .MockIisExpressConfig_RealDocumentInitialized(configPath);

                var config = new IisExpressConfig(configPath, _mock.FileSystem.Object);

                // Act
                config.SetDefaultApplicationPool("mypool");

                //Assert
                var xappPoolDefaults = config.Root.Document.Descendants("applicationDefaults").First();
                Assert.Equal(@"mypool", xappPoolDefaults.Attribute("applicationPool").Value);
            }
        }

        public class StoreSchema
        {
            private WebServerMockGenerator _mock;

            public StoreSchema()
            {
                _mock = new WebServerMockGenerator();
            }

            [Fact]
            public void WhenStoreSchema_ShouldStore()
            {
                // Arrange 
                const string configPath = "app.config";
                _mock.MockIisExpressConfig_ApplicationConfigFound(configPath)
                    .MockIisExpressConfig_RealDocumentInitialized(configPath)
                    .MockIisExpressConfig_StoreSchema(configPath);

                var config = new IisExpressConfig(configPath, _mock.FileSystem.Object);

                // Act, Assert
                config.StoreSchema();
            }
        }
    }
}
