using Moq;
using NuGet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestEasy.Nuget.Test.Mocks;
using TestEasy.NuGet;
using Xunit;
using Xunit.Extensions;

namespace TestEasy.Nuget.Test
{
    public class WebNuGetProjectTests
    {
        public class InstallPackage
        {
            [Fact]
            public void InstallToPackageManagerBeforeAddingToProjectManager()
            {
                var package = new MockPackage();
                bool calledPackageManager = false;
                bool calledProjectManagerAfterPackageManager = false;

                var mockPackageMan = new Mock<IPackageManager>();
                mockPackageMan.Setup(packMan => packMan.InstallPackage(It.IsAny<IPackage>(), It.IsAny<bool>(), It.IsAny<bool>())).Callback(() => calledPackageManager = true);

                var mockProjectMan = new Mock<IProjectManager>();
                mockProjectMan.Setup(projMan => projMan.AddPackageReference(It.IsAny<IPackage>(), false, true)).Callback(() => calledProjectManagerAfterPackageManager = calledPackageManager);

                Dependencies.NuGetPackageManagerFactory = MockGenerator.CreatePackageManagerFactory(mockPackageMan.Object);
                Dependencies.NuGetProjectManagerFactory = MockGenerator.CreateProjectManagerFactory(mockProjectMan.Object);

                // act
                WebNuGetProject project = new WebNuGetProject(new string[] {}, @"C:\DummyPath", null, MockGenerator.CreateWebProjectSystemFactory());
                var warnings = project.InstallPackage(package);

                // assert
                Assert.True(calledProjectManagerAfterPackageManager);
            }
        }

        public class UpdatePackage
        {
            [Fact]
            public void InstallNewPackageToPackageManagerBeforeProjectManager()
            {
                var package = new MockPackage();
                var mockRepository = new MockPackageRepository();
                mockRepository.AddPackage(package);
                bool calledPackageManager = false;
                bool calledProjectManagerAfterPackageManager = false;

                var mockPackageMan = MockGenerator.CreateMockPackageManager(mockRepository, mockRepository);
                mockPackageMan.Setup(packMan => packMan.InstallPackage(package, It.IsAny<bool>(), It.IsAny<bool>()))
                    .Callback(() => calledPackageManager = true);

                var mockProjectMan = MockGenerator.CreateMockProjectManager(mockRepository, mockRepository);
                mockProjectMan.Setup(projMan => projMan.UpdatePackageReference(package.Id, package.Version, It.IsAny<bool>(), It.IsAny<bool>()))
                    .Callback(() => calledProjectManagerAfterPackageManager = calledPackageManager);

                Dependencies.NuGetPackageManagerFactory = MockGenerator.CreatePackageManagerFactory(mockPackageMan.Object);
                Dependencies.NuGetProjectManagerFactory = MockGenerator.CreateProjectManagerFactory(mockProjectMan.Object);

                // act
                WebNuGetProject project = new WebNuGetProject(new string[] {}, @"C:\DummyPath", null, MockGenerator.CreateWebProjectSystemFactory());
                var warnings = project.UpdatePackage(package);

                // assert
                Assert.True(calledProjectManagerAfterPackageManager);
            }

            [Fact]
            public void OldVersionOfPackageUninstalled()
            {
                var oldPackage = new MockPackage();
                var package = new MockPackage { Version = new SemanticVersion("2.0") };

                var mockRepository = new MockPackageRepository();
                mockRepository.AddPackage(oldPackage);

                var mockPackageMan = MockGenerator.CreateMockPackageManager(mockRepository, mockRepository);
                mockPackageMan.Setup(packMan => packMan.UninstallPackage(oldPackage, It.IsAny<bool>(), It.IsAny<bool>()))
                    .Verifiable();
                var mockProjectMan = MockGenerator.CreateMockProjectManager(mockRepository, mockRepository);

                Dependencies.NuGetPackageManagerFactory = MockGenerator.CreatePackageManagerFactory(mockPackageMan.Object);
                Dependencies.NuGetProjectManagerFactory = MockGenerator.CreateProjectManagerFactory(mockProjectMan.Object);

                // act
                WebNuGetProject project = new WebNuGetProject(new string[] { }, @"C:\DummyPath", null, MockGenerator.CreateWebProjectSystemFactory());
                var warnings = project.UpdatePackage(package);

                // assert
                mockPackageMan.Verify();
            }

            [Fact]
            public void UninstallOldPackageFails_HandleGracefully()
            {
                var oldPackage = new MockPackage();
                var package = new MockPackage { Version = new SemanticVersion("2.0") };

                var mockRepository = new MockPackageRepository();
                mockRepository.AddPackage(oldPackage);

                var mockPackageMan = MockGenerator.CreateMockPackageManager(mockRepository, mockRepository);
                mockPackageMan.Setup(packMan => packMan.UninstallPackage(oldPackage, It.IsAny<bool>(), It.IsAny<bool>()))
                    .Throws(new InvalidOperationException("Dummy message"));
                mockPackageMan.SetupProperty(packMan => packMan.Logger, new TestEasy.NuGet.WebNuGetProject.ErrorLogger());
                var mockProjectMan = MockGenerator.CreateMockProjectManager(mockRepository, mockRepository);

                Dependencies.NuGetPackageManagerFactory = MockGenerator.CreatePackageManagerFactory(mockPackageMan.Object);
                Dependencies.NuGetProjectManagerFactory = MockGenerator.CreateProjectManagerFactory(mockProjectMan.Object);

                // act
                WebNuGetProject project = new WebNuGetProject(new string[] { }, @"C:\DummyPath", null, MockGenerator.CreateWebProjectSystemFactory());
                var warnings = project.UpdatePackage(package);

                // assert
                Assert.Contains("Package MockPackage.1.0 could not be uninstalled: Dummy message", warnings);
            }

        }

        public class UninstallPackage
        {
            [Fact]
            public void RemoveFromPackageManagerAfterRemovingFromProjectManager()
            {
                var package = new MockPackage();
                bool calledProjectManager = false;
                bool calledPackageManagerAfterProjectManager = false;

                var mockPackageMan = new Mock<IPackageManager>() { DefaultValue = DefaultValue.Mock };
                mockPackageMan.Setup(packMan => packMan.UninstallPackage(package, It.IsAny<bool>(), It.IsAny<bool>()))
                    .Callback(() => calledPackageManagerAfterProjectManager = calledProjectManager);

                var mockProjectMan = new Mock<IProjectManager>() { DefaultValue = DefaultValue.Mock };
                mockProjectMan.Setup(projMan => projMan.RemovePackageReference(It.IsAny<IPackage>(), false, true))
                    .Callback(() => calledProjectManager = true);

                Dependencies.NuGetPackageManagerFactory = MockGenerator.CreatePackageManagerFactory(mockPackageMan.Object);
                Dependencies.NuGetProjectManagerFactory = MockGenerator.CreateProjectManagerFactory(mockProjectMan.Object);

                // act
                WebNuGetProject project = new WebNuGetProject(new string[] { }, @"C:\DummyPath", null, MockGenerator.CreateWebProjectSystemFactory());
                var warnings = project.UninstallPackage(package, true);

                // assert
                Assert.True(calledPackageManagerAfterProjectManager);
            }
        }

        public class GetPackages
        {
            [Fact]
            public void NoMatchingPackage()
            {
                // arrange
                var repoPackages = new IPackage[] { new MockPackage("A", "1.0"), new MockPackage("B", "2.0") };
                var searchTerm = "C";
                var expected = new IPackage[] { };

                // act
                var results = WebNuGetProject.GetPackages(repoPackages.AsQueryable<IPackage>(), searchTerm);

                // assert
                Assert.Equal(expected, results.AsEnumerable<IPackage>(), new MockPackage.StubPackageComparer());
            }

            [Fact]
            public void MatchingPackage()
            {
                // arrange
                var repoPackages = new IPackage[] { new MockPackage("A", "1.0"), new MockPackage("B", "2.0") };
                var searchTerm = "A";
                var expected = new IPackage[] { new MockPackage("A", "1.0") };

                // act
                var results = WebNuGetProject.GetPackages(repoPackages.AsQueryable<IPackage>(), searchTerm);

                // assert
                Assert.Equal(expected, results.AsEnumerable<IPackage>(), new MockPackage.StubPackageComparer());
            }

            [Fact]
            public void ReturnMultipleVersionsOfPackage()
            {
                // arrange
                var repoPackages = new IPackage[] { new MockPackage("A", "1.0"), new MockPackage("A", "2.0"), new MockPackage("B", "2.0") };
                var searchTerm = "A";
                var expected = new IPackage[] { new MockPackage("A", "1.0"), new MockPackage("A", "2.0") };

                // act
                var results = WebNuGetProject.GetPackages(repoPackages.AsQueryable<IPackage>(), searchTerm);

                // assert
                Assert.Equal(expected, results.AsEnumerable<IPackage>(), new MockPackage.StubPackageComparer());
            }

            [Fact]
            public void ReturnMultiplePackages()
            {
                // arrange
                var repoPackages = new IPackage[] { new MockPackage("A", "1.0"), new MockPackage("AB", "2.0"), new MockPackage("B", "2.0") };
                var searchTerm = "A";
                var expected = new IPackage[] { new MockPackage("A", "1.0"), new MockPackage("AB", "2.0") };

                // act
                var results = WebNuGetProject.GetPackages(repoPackages.AsQueryable<IPackage>(), searchTerm);

                // assert
                Assert.Equal(expected, results.AsEnumerable<IPackage>(), new MockPackage.StubPackageComparer());
            }

            [Fact]
            public void EmptySearchTerm_ReturnAllPackages()
            {
                var repoPackages = new IPackage[] { new MockPackage("A", "1.0"), new MockPackage("AB", "2.0")};
                var expected = new IPackage[] { new MockPackage("A", "1.0"), new MockPackage("AB", "2.0") };

                // act
                var results = WebNuGetProject.GetPackages(repoPackages.AsQueryable<IPackage>(), null);
                // assert
                Assert.Equal(expected, results.AsEnumerable<IPackage>(), new MockPackage.StubPackageComparer());

                // act
                results = WebNuGetProject.GetPackages(repoPackages.AsQueryable<IPackage>(), string.Empty);
                // assert
                Assert.Equal(expected, results.AsEnumerable<IPackage>(), new MockPackage.StubPackageComparer());
            }
        }

        public class GetInstalledPackages
        {
            [Fact]
            public void EmptySearchTerms_ReturnsAllLocalPackages()
            {
                var packageA = new MockPackage { Id = "A" };
                var packageB = new MockPackage { Id = "B" };

                var mockRepo = new MockPackageRepository();
                mockRepo.AddPackage(packageA);
                mockRepo.AddPackage(packageB);

                Dependencies.NuGetProjectManagerFactory = MockGenerator.CreateProjectManagerFactory(MockGenerator.CreateMockProjectManager(new MockPackageRepository(), mockRepo).Object);

                // act
                var project = new WebNuGetProject(new string[] {"http://dummyFeed"}, @"C:\DummyPath", null, MockGenerator.CreateWebProjectSystemFactory());
                var results = project.GetInstalledPackages("");

                Assert.Equal(new IPackage[] { packageA, packageB}, results);
            }

            [Fact]
            public void DoesNotReturnPackagesFromRemote()
            {
                var packageA = new MockPackage { Id = "A" };
                var packageB = new MockPackage { Id = "B" };
                var packageAB = new MockPackage { Id = "AB" };

                var mockLocalRepo = new MockPackageRepository();
                mockLocalRepo.AddPackage(packageA);
                mockLocalRepo.AddPackage(packageB);
                var mockRemoteRepo = new MockPackageRepository();
                mockRemoteRepo.AddPackage(packageAB);

                Dependencies.NuGetProjectManagerFactory = MockGenerator.CreateProjectManagerFactory(MockGenerator.CreateMockProjectManager(mockRemoteRepo, mockLocalRepo).Object);

                // act
                var project = new WebNuGetProject(new string[] {"http://dummyFeed"}, @"C:\DummyPath", null, MockGenerator.CreateWebProjectSystemFactory());
                var results = project.GetInstalledPackages("A");

                Assert.Equal(new IPackage[] { packageA }, results);
            }
        }

        public class GetRemotePackages
        {
            [Fact]
            public void EmptySearchTerms_ReturnsAllRemotePackages()
            {
                var packageA = new MockPackage { Id = "A" };
                var packageB = new MockPackage { Id = "B" };

                var mockRepo = new MockPackageRepository();
                mockRepo.AddPackage(packageA);
                mockRepo.AddPackage(packageB);

                Dependencies.NuGetProjectManagerFactory = MockGenerator.CreateProjectManagerFactory(MockGenerator.CreateMockProjectManager(mockRepo, new MockPackageRepository()).Object);

                // act
                var project = new WebNuGetProject(new string[] { "http://dummyFeed" }, @"C:\DummyPath", null, MockGenerator.CreateWebProjectSystemFactory());
                var results = project.GetRemotePackages("");

                Assert.Equal(new IPackage[] { packageA, packageB }, results);
            }

            [Fact]
            public void OrderedByDownloadCountFirst()
            {
                var packageA = new MockPackage { Id = "A", DownloadCount = 1 };
                var packageB = new MockPackage { Id = "B", DownloadCount = 2 };

                var mockRepo = new MockPackageRepository();
                mockRepo.AddPackage(packageA);
                mockRepo.AddPackage(packageB);

                Dependencies.NuGetProjectManagerFactory = MockGenerator.CreateProjectManagerFactory(MockGenerator.CreateMockProjectManager(mockRepo, new MockPackageRepository()).Object);

                // act
                var project = new WebNuGetProject(new string[] { "http://dummyFeed" }, @"C:\DummyPath", null, MockGenerator.CreateWebProjectSystemFactory());
                var results = project.GetRemotePackages("");

                Assert.Equal(new IPackage[] { packageB, packageA }, results);

                packageA.DownloadCount = 2;

                results = project.GetRemotePackages("");
                Assert.Equal(new IPackage[] { packageA, packageB}, results);

            }

            [Fact]
            public void DoesNotReturnPackagesFromLocal()
            {
                var packageA = new MockPackage { Id = "A" };
                var packageB = new MockPackage { Id = "B" };
                var packageAB = new MockPackage { Id = "AB" };
                
                var mockRemoteRepo = new MockPackageRepository();
                mockRemoteRepo.AddPackage(packageA);
                mockRemoteRepo.AddPackage(packageB);
                var mockLocalRepo = new MockPackageRepository();
                mockLocalRepo.AddPackage(packageAB);

                Dependencies.NuGetProjectManagerFactory = MockGenerator.CreateProjectManagerFactory(MockGenerator.CreateMockProjectManager(mockRemoteRepo, mockLocalRepo).Object);

                // act
                var project = new WebNuGetProject(new string[] { "http://dummyFeed" }, @"C:\DummyPath", null, MockGenerator.CreateWebProjectSystemFactory());
                var results = project.GetRemotePackages("A");

                Assert.Equal(new IPackage[] { packageA }, results);
            }
        }
    }
}
