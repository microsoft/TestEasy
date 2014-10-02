using Moq;
using NuGet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestEasy.Core.Abstractions;
using TestEasy.Nuget.Test.Mocks;
using TestEasy.NuGet;
using Xunit;
using Xunit.Extensions;

namespace TestEasy.Nuget.Test
{
    public class NuGetManagerTests
    {
        public class InstallPackage
        {
            [Fact]
            public void PhysicalPathIsEmpty_Throws()
            {
                var manager = new NuGetManager();

                var ex = Assert.Throws<ArgumentNullException>(() => manager.InstallPackage(null, "DummyPackage"));
                Assert.Contains("appPhysicalPath", ex.Message);

                ex = Assert.Throws<ArgumentNullException>(() => manager.InstallPackage(string.Empty, "DummyPackage"));
                Assert.Contains("appPhysicalPath", ex.Message);
            }

            [Fact]
            public void PackageNameIsEmpty_Throws()
            {
                var manager = new NuGetManager();

                var ex = Assert.Throws<ArgumentNullException>(() => manager.InstallPackage("DummyPath", null));
                Assert.Contains("packageName", ex.Message);

                ex = Assert.Throws<ArgumentNullException>(() => manager.InstallPackage("DummyPath", string.Empty));
                Assert.Contains("packageName", ex.Message);
            }
            
            [Fact]
            public void SourcesEmpty_NugetDefaultFeedUsed()
            {
                // arrange
                var mockNugetCore = new Mock<INuGetCore>();
                mockNugetCore.Setup(n => n.InstallPackage("DummyPath", "DummyPackage", It.Is<string>(s => s.Contains(NuGetManager.NUGET_DEFAULT_SOURCE)), "", null))
                    .Returns(() => null)
                    .Verifiable();
                var mockFileSystem = new Mock<Core.Abstractions.IFileSystem>();

                // act
                var manager = new NuGetManager(mockNugetCore.Object, mockFileSystem.Object);
                manager.InstallPackage("DummyPath", "DummyPackage");

                // assert
                mockNugetCore.Verify();
            }

            [Fact]
            public void SourcesProvided_NugetDefaultFeedIncluded()
            {
                // arrange
                var mockNugetCore = new Mock<INuGetCore>();
                mockNugetCore.Setup(n => n.InstallPackage("DummyPath", "DummyPackage", It.Is<string>(s => s.Contains(NuGetManager.NUGET_DEFAULT_SOURCE)), "", null))
                    .Returns(() => null)
                    .Verifiable();
                var mockFileSystem = new Mock<Core.Abstractions.IFileSystem>();

                // act
                var manager = new NuGetManager(mockNugetCore.Object, mockFileSystem.Object);
                manager.InstallPackage("DummyPath", "DummyPackage", "http://DummyFeed");
                
                // assert
                mockNugetCore.Verify();
            }
        }
        public class InstallPackages
        {
            [Fact]
            public void PackagesIsNull_Throws()
            {
                var manager = new NuGetManager();

                Assert.Throws<ArgumentNullException>(() => manager.InstallPackages("DummyPath", (IEnumerable<PackageName>)null));
            }

            [Fact]
            public void InstallAllProvidedPackages()
            {
                // arrange
                var mockNugetCore = new Mock<INuGetCore>();
                mockNugetCore.Setup(n => n.InstallPackage("DummyPath", "FooPackage", It.IsAny<string>(), "1.0", null))
                    .Returns(Enumerable.Empty<string>).Verifiable();
                mockNugetCore.Setup(n => n.InstallPackage("DummyPath", "BarPackage", It.IsAny<string>(), "2.0", null))
                    .Returns(Enumerable.Empty<string>).Verifiable();
                var mockFileSystem = new Mock<Core.Abstractions.IFileSystem>();

                var packagesToInstall = new List<PackageName>() {
                    new PackageName("FooPackage", SemanticVersion.Parse("1.0")),
                    new PackageName("BarPackage", SemanticVersion.Parse("2.0"))
                };

                // act
                var manager = new NuGetManager(mockNugetCore.Object, mockFileSystem.Object);
                manager.InstallPackages("DummyPath", packagesToInstall);

                // assert
                mockNugetCore.Verify();
            }

            [Fact]
            public void NullVersionConvertedToEmptyString()
            {
                // arrange
                var mockNugetCore = new Mock<INuGetCore>();
                mockNugetCore.Setup(n => n.InstallPackage("DummyPath", "FooPackage", It.IsAny<string>(), "", null))
                    .Returns(Enumerable.Empty<string>).Verifiable();
                var mockFileSystem = new Mock<Core.Abstractions.IFileSystem>();

                var packagesToInstall = new List<PackageName>() {
                    new PackageName("FooPackage", null),
                };

                // act
                var manager = new NuGetManager(mockNugetCore.Object, mockFileSystem.Object);
                manager.InstallPackages("DummyPath", packagesToInstall);

                // assert
                mockNugetCore.Verify();
            }

            [Fact]
            public void PackagesConfigPathIsEmpty_Throws()
            {
                var manager = new NuGetManager();
                Assert.Throws<ArgumentNullException>(() => manager.InstallPackages("DummyPath", (string)null));
                Assert.Throws<ArgumentNullException>(() => manager.InstallPackages("DummyPath", ""));
            }

            [Fact]
            public void PackagesConfigPathDoesNotExist_Throws()
            {
                var mockNugetCore = new Mock<INuGetCore>();
                var mockFileSystem = new Mock<Core.Abstractions.IFileSystem>(MockBehavior.Strict); // strict ensures we don't use any other FileSystem APIs
                mockFileSystem.Setup(fs => fs.FileExists("DummyPackagesConfigPath")).Returns(false).Verifiable();

                var manager = new NuGetManager(mockNugetCore.Object, mockFileSystem.Object);
                Assert.Throws<FileNotFoundException>(() => manager.InstallPackages("DummyPath", "DummyPackagesConfigPath"));
                mockFileSystem.Verify();
            }

            [Theory]
            [InlineData(@"<packages></packages>", new string[] { })]
            [InlineData(@"<packages><package id=""FooPackage"" version=""1.0"" /></packages>", new string[] { "FooPackage|1.0" })]
            [InlineData(@"<packages><package id=""FooPackage"" version=""1.0"" /><package id=""BarPackage"" version=""2.0"" /><notapackage id=""NotAPackage"" /></packages>", new string[] { "FooPackage|1.0", "BarPackage|2.0" })]
            [InlineData(@"<packages><subnode><package id=""FooPackage"" version=""1.0"" /></subnode><package id=""BarPackage"" version=""2.0"" /></packages>", new string[] { "FooPackage|1.0", "BarPackage|2.0" })]
            public void ReadFromPackagesConfig(string configContent, string[] packagesToExpect)
            {
                // arrange
                var mockNugetCore = new Mock<INuGetCore>();
                foreach (var package in packagesToExpect)
                {
                    string packageId = package.Split('|')[0];
                    string packageVer = package.Split('|')[1];
                    mockNugetCore.Setup(n => n.InstallPackage("DummyPath", packageId, It.IsAny<string>(), It.IsAny<string>(), null))
                        .Returns(Enumerable.Empty<string>).Verifiable();
                }
                var mockFileSystem = new Mock<Core.Abstractions.IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(fs => fs.DirectorySetAttribute("DummyPath", FileAttributes.Normal));
                mockFileSystem.Setup(fs => fs.FileExists("DummyPackagesConfigPath")).Returns(true);
                mockFileSystem.Setup(fs => fs.FileReadAllText("DummyPackagesConfigPath")).Returns(configContent);

                // act
                var manager = new NuGetManager(mockNugetCore.Object, mockFileSystem.Object);
                manager.InstallPackages("DummyPath", "DummyPackagesConfigPath", binariesOnly:false);

                // assert
                mockNugetCore.Verify();
            }

            [Theory]
            [InlineData(@"<packages><package id=""FooPackage"" version=""1.0"" /></packages>", new string[] { "FooPackage|1.0" }, "")]
            [InlineData(@"<packages><package id=""FooPackage"" version=""1.0"" source=""dummyFeed"" /><package id=""BarPackage"" source=""dummyFeed2"" version=""2.0"" /><notapackage id=""NotAPackage"" /></packages>", new string[] { "FooPackage|1.0", "BarPackage|2.0" }, "dummyFeed;dummyFeed2")]
            public void ReadFromPackagesConfig_UseCustomSources(string configContent, string[] packagesToExpect, string customSources)
            {
                // arrange
                var mockNugetCore = new Mock<INuGetCore>();
                foreach (var package in packagesToExpect)
                {
                    string packageId = package.Split('|')[0];
                    string packageVer = package.Split('|')[1];
                    mockNugetCore.Setup(n => n.InstallPackage("DummyPath", packageId, It.Is<string>(s => s.Contains(customSources)), It.IsAny<string>(), null))
                        .Returns(Enumerable.Empty<string>).Verifiable();
                }
                var mockFileSystem = new Mock<Core.Abstractions.IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(fs => fs.DirectorySetAttribute("DummyPath", FileAttributes.Normal));
                mockFileSystem.Setup(fs => fs.FileExists("DummyPackagesConfigPath")).Returns(true);
                mockFileSystem.Setup(fs => fs.FileReadAllText("DummyPackagesConfigPath")).Returns(configContent);

                // act
                var manager = new NuGetManager(mockNugetCore.Object, mockFileSystem.Object);
                manager.InstallPackages("DummyPath", "DummyPackagesConfigPath", binariesOnly: false);

                // assert
                mockNugetCore.Verify();
            }

        }

        public class UpdatePackage
        {
            [Fact]
            public void PhysicalPathIsEmpty_Throws()
            {
                var manager = new NuGetManager();

                var ex = Assert.Throws<ArgumentNullException>(() => manager.UpdatePackage(null, "DummyPackage"));
                Assert.Contains("appPhysicalPath", ex.Message);

                ex = Assert.Throws<ArgumentNullException>(() => manager.UpdatePackage(string.Empty, "DummyPackage"));
                Assert.Contains("appPhysicalPath", ex.Message);
            }

            [Fact]
            public void PackageNameIsEmpty_Throws()
            {
                var manager = new NuGetManager();

                var ex = Assert.Throws<ArgumentNullException>(() => manager.UpdatePackage("DummyPath", null));
                Assert.Contains("packageName", ex.Message);

                ex = Assert.Throws<ArgumentNullException>(() => manager.UpdatePackage("DummyPath", string.Empty));
                Assert.Contains("packageName", ex.Message);
            }

            [Fact]
            public void SourcesEmpty_NugetDefaultFeedUsed()
            {
                // arrange
                var mockNugetCore = new Mock<INuGetCore>();
                mockNugetCore.Setup(n => n.UpdatePackage("DummyPath", "DummyPackage", It.Is<string>(s => s.Contains(NuGetManager.NUGET_DEFAULT_SOURCE)), "", null))
                    .Returns(() => null).Verifiable();
                var mockFileSystem = new Mock<Core.Abstractions.IFileSystem>();

                // act
                var manager = new NuGetManager(mockNugetCore.Object, mockFileSystem.Object);
                manager.UpdatePackage("DummyPath", "DummyPackage");

                // assert
                mockNugetCore.Verify();
            }

            [Fact]
            public void SourcesProvided_NugetDefaultFeedIncluded()
            {
                // arrange
                var mockNugetCore = new Mock<INuGetCore>(MockBehavior.Strict);
                mockNugetCore.Setup(n => n.UpdatePackage("DummyPath", "DummyPackage", It.Is<string>(s => s.Contains(NuGetManager.NUGET_DEFAULT_SOURCE)), "", null))
                    .Returns(() => null).Verifiable();
                var mockFileSystem = new Mock<Core.Abstractions.IFileSystem>();

                // act
                var manager = new NuGetManager(mockNugetCore.Object, mockFileSystem.Object);
                manager.UpdatePackage("DummyPath", "DummyPackage", "http://DummyFeed");

                // assert
                mockNugetCore.Verify();
            }
        }
        public class UpdatePackages
        {
            [Fact]
            public void PackagesIsNull_Throws()
            {
                var manager = new NuGetManager();

                Assert.Throws<ArgumentNullException>(() => manager.UpdatePackages("DummyPath", (IEnumerable<PackageName>)null));
            }

            [Fact]
            public void InstallAllProvidedPackages()
            {
                // arrange
                var mockNugetCore = new Mock<INuGetCore>();
                mockNugetCore.Setup(n => n.UpdatePackage("DummyPath", "FooPackage", It.IsAny<string>(), "1.0", null))
                    .Returns(Enumerable.Empty<string>).Verifiable();
                mockNugetCore.Setup(n => n.UpdatePackage("DummyPath", "BarPackage", It.IsAny<string>(), "2.0", null))
                    .Returns(Enumerable.Empty<string>).Verifiable();
                var mockFileSystem = new Mock<Core.Abstractions.IFileSystem>();

                var packagesToUpdate = new List<PackageName>() {
                    new PackageName("FooPackage", SemanticVersion.Parse("1.0")),
                    new PackageName("BarPackage", SemanticVersion.Parse("2.0"))
                };

                // act
                var manager = new NuGetManager(mockNugetCore.Object, mockFileSystem.Object);
                manager.UpdatePackages("DummyPath", packagesToUpdate);

                // assert
                mockNugetCore.Verify();
            }

            [Fact]
            public void NullVersionConvertedToEmptyString()
            {
                // arrange
                var mockNugetCore = new Mock<INuGetCore>();
                mockNugetCore.Setup(n => n.UpdatePackage("DummyPath", "FooPackage", It.IsAny<string>(), "", null))
                    .Returns(Enumerable.Empty<string>).Verifiable();
                var mockFileSystem = new Mock<Core.Abstractions.IFileSystem>();

                var packagesToUpdate = new List<PackageName>() {
                    new PackageName("FooPackage", null),
                };

                // act
                var manager = new NuGetManager(mockNugetCore.Object, mockFileSystem.Object);
                manager.UpdatePackages("DummyPath", packagesToUpdate);

                // assert
                mockNugetCore.Verify();
            }
        }

        public class PerformNugetAction
        {
            [Fact]
            public void BinariesOnlyFalse_WebConfigNotPreserved()
            {
                var mockNugetCore = new Mock<INuGetCore>();
                var mockFileSystem = new Mock<Core.Abstractions.IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(fs => fs.DirectorySetAttribute("DummyPath", FileAttributes.Normal));

                // act
                // assert we don't call any other FileSystem APIs handled by MockBehavior.Strict
                var manager = new NuGetManager(mockNugetCore.Object, mockFileSystem.Object);
                manager.InstallPackage(@"DummyPath", "DummyPackage", binariesOnly: false);
            }

            [Fact]
            public void BinariesOnlyTrue_WebConfigIsPreserved()
            {
                // arrange
                var mockNugetCore = new Mock<INuGetCore>();
                var mockFileSystem = new Mock<Core.Abstractions.IFileSystem>();
                mockFileSystem.Setup(fs => fs.DirectorySetAttribute("DummyPath", FileAttributes.Normal));
                mockFileSystem.Setup(fs => fs.FileExists(@"DummyPath\web.config")).Returns(true).Verifiable();
                mockFileSystem.Setup(fs => fs.FileCopy(@"DummyPath\web.config", @"DummyPath\web.config.temp", true)).Verifiable();
                mockFileSystem.Setup(fs => fs.FileExists(@"DummyPath\web.config.temp")).Returns(true).Verifiable();
                mockFileSystem.Setup(fs => fs.FileCopy(@"DummyPath\web.config.temp", @"DummyPath\web.config", true)).Verifiable();
                mockFileSystem.Setup(fs => fs.FileDelete(@"DummyPath\web.config.temp")).Verifiable();

                // act
                var manager = new NuGetManager(mockNugetCore.Object, mockFileSystem.Object);
                manager.InstallPackage(@"DummyPath", "DummyPackage", binariesOnly: true);

                // assert 
                mockFileSystem.Verify();
            }

            [Fact]
            public void BinariesOnlyTrue_WebConfigDoesntExist()
            {
                // arrange
                var mockNugetCore = new Mock<INuGetCore>();
                var mockFileSystem = new Mock<Core.Abstractions.IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(fs => fs.DirectorySetAttribute("DummyPath", FileAttributes.Normal));
                mockFileSystem.Setup(fs => fs.FileExists(@"DummyPath\web.config")).Returns(false).Verifiable();
                mockFileSystem.Setup(fs => fs.FileExists(@"DummyPath\web.config.temp")).Returns(false).Verifiable();

                // act
                var manager = new NuGetManager(mockNugetCore.Object, mockFileSystem.Object);
                manager.InstallPackage(@"DummyPath", "DummyPackage", binariesOnly: true);

                // assert
                mockFileSystem.Verify();
                // assert we don't call any other FileSystem APIs handled by MockBehavior.Strict
            }
        }
    }
}
