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

namespace TestEasy.Nuget.Test
{
    public class NuGetCoreTests
    {
        public class InstallPackage
        {
            [Fact]
            public void PackageAvailableFromRemote_InstallAddsPackageToLocalRepository()
            {
                // arrange
                var package = new MockPackage();
                var mockLocalRepository = new MockPackageRepository();
                var mockRemoteRepository = new MockPackageRepository();
                mockRemoteRepository.AddPackage(package);

                var mockPackageManager = new Mock<IPackageManager>();
                var mockProjectManager = MockGenerator.CreateMockProjectManager(mockRemoteRepository, mockLocalRepository);
                mockProjectManager.Setup(pm => pm.AddPackageReference(package, It.IsAny<bool>(), It.IsAny<bool>())).Verifiable();

                Dependencies.NuGetPackageManagerFactory = MockGenerator.CreatePackageManagerFactory(mockPackageManager.Object);
                Dependencies.NuGetProjectManagerFactory = MockGenerator.CreateProjectManagerFactory(mockProjectManager.Object);
                Dependencies.NuGetProjectSystemFactory = MockGenerator.CreateProjectSystemFactory();

                // act
                NuGetCore core = new NuGetCore();
                var warnings = core.InstallPackage(@"C:\DummyPath", MockPackage.DefaultId, "http://dummyFeed", MockPackage.DefaultVersion.ToString());

                // assert
                mockProjectManager.Verify();
            }

            [Fact]
            public void PackageAvailableFromLocal_InstallSucceeds()
            {
                // arrange
                var package = new MockPackage();
                var mockLocalRepository = new MockPackageRepository();
                var mockRemoteRepository = new MockPackageRepository();
                mockLocalRepository.AddPackage(package);

                var mockPackageManager = new Mock<IPackageManager>();
                var mockProjectManager = MockGenerator.CreateMockProjectManager(mockRemoteRepository, mockLocalRepository);
                mockProjectManager.Setup(pm => pm.AddPackageReference(package, It.IsAny<bool>(), It.IsAny<bool>())).Verifiable();

                Dependencies.NuGetPackageManagerFactory = MockGenerator.CreatePackageManagerFactory(mockPackageManager.Object);
                Dependencies.NuGetProjectManagerFactory = MockGenerator.CreateProjectManagerFactory(mockProjectManager.Object);
                Dependencies.NuGetProjectSystemFactory = MockGenerator.CreateProjectSystemFactory();

                // act
                NuGetCore core = new NuGetCore();
                var warnings = core.InstallPackage(@"C:\DummyPath", MockPackage.DefaultId, "http://dummyFeed", MockPackage.DefaultVersion.ToString());

                // assert
                mockProjectManager.Verify();
            }

            [Fact]
            public void PackageNotFound_ThrowsException()
            {
                // arrange
                var mockLocalRepository = new MockPackageRepository();
                var mockRemoteRepository = new MockPackageRepository();

                var mockPackageManager = new Mock<IPackageManager>();
                var mockProjectManager = MockGenerator.CreateMockProjectManager(mockRemoteRepository, mockLocalRepository);

                Dependencies.NuGetPackageManagerFactory = MockGenerator.CreatePackageManagerFactory(mockPackageManager.Object);
                Dependencies.NuGetProjectManagerFactory = MockGenerator.CreateProjectManagerFactory(mockProjectManager.Object);
                Dependencies.NuGetProjectSystemFactory = MockGenerator.CreateProjectSystemFactory();

                string dummyPackage = "DummyPackage";
                string dummyVersion = "1.0";

                // act
                // assert
                NuGetCore core = new NuGetCore();
                var ex = Assert.Throws<Exception>(() => core.InstallPackage(@"C:\DummyPath", dummyPackage, "", dummyVersion));
                Assert.Contains(string.Format("No package named {0}.{1} found at location", dummyPackage, dummyVersion), ex.Message);
            }
        }

        public class UpdatePackage
        {
            [Fact]
            public void PackageAvailableFromRemote_UpdateSucceeds()
            {
                // arrange
                var updatedPackage = new MockPackage { Version = new SemanticVersion("2.0") };
                var mockLocalRepository = new MockPackageRepository();
                var mockRemoteRepository = new MockPackageRepository();
                mockLocalRepository.AddPackage(new MockPackage());
                mockRemoteRepository.AddPackage(updatedPackage);

                var mockPackageManager = MockGenerator.CreateMockPackageManager(mockRemoteRepository, mockLocalRepository);
                var mockProjectManager = MockGenerator.CreateMockProjectManager(mockRemoteRepository, mockLocalRepository);
                mockProjectManager.Setup(pm => pm.UpdatePackageReference(updatedPackage.Id, updatedPackage.Version, It.IsAny<bool>(), It.IsAny<bool>())).Verifiable();

                Dependencies.NuGetPackageManagerFactory = MockGenerator.CreatePackageManagerFactory(mockPackageManager.Object);
                Dependencies.NuGetProjectManagerFactory = MockGenerator.CreateProjectManagerFactory(mockProjectManager.Object);
                Dependencies.NuGetProjectSystemFactory = MockGenerator.CreateProjectSystemFactory();

                // act
                NuGetCore core = new NuGetCore();
                var warnings = core.UpdatePackage(@"C:\DummyPath", MockPackage.DefaultId, "http://dummyFeed", "2.0");

                // assert
                mockProjectManager.Verify();
            }

            [Fact]
            public void DontSpecifyVersion_UpdatesToLatestPackage()
            {
                var latestPackage = new MockPackage { Version = new SemanticVersion("3.0") };
                var mockLocalRepository = new MockPackageRepository();
                var mockRemoteRepository = new MockPackageRepository();
                mockLocalRepository.AddPackage(new MockPackage());
                mockRemoteRepository.AddPackage(new MockPackage { Version = new SemanticVersion("2.0") });
                mockRemoteRepository.AddPackage(latestPackage);

                var mockPackageManager = MockGenerator.CreateMockPackageManager(mockRemoteRepository, mockLocalRepository);
                var mockProjectManager = MockGenerator.CreateMockProjectManager(mockRemoteRepository, mockLocalRepository);
                mockProjectManager.Setup(pm => pm.UpdatePackageReference(latestPackage.Id, latestPackage.Version, It.IsAny<bool>(), It.IsAny<bool>())).Verifiable();

                Dependencies.NuGetPackageManagerFactory = MockGenerator.CreatePackageManagerFactory(mockPackageManager.Object);
                Dependencies.NuGetProjectManagerFactory = MockGenerator.CreateProjectManagerFactory(mockProjectManager.Object);
                Dependencies.NuGetProjectSystemFactory = MockGenerator.CreateProjectSystemFactory();

                // act
                NuGetCore core = new NuGetCore();
                var warnings = core.UpdatePackage(@"C:\DummyPath", MockPackage.DefaultId, "http://dummyFeed", null);

                // assert
                mockProjectManager.Verify();
            }

            [Fact]
            public void SpecifyVersion_UpdatesToSpecifiedPackage()
            {
                // arrange
                var targetPackage = new MockPackage { Version = new SemanticVersion("2.0") };
                var mockLocalRepository = new MockPackageRepository();
                var mockRemoteRepository = new MockPackageRepository();
                mockLocalRepository.AddPackage(new MockPackage());
                mockRemoteRepository.AddPackage(targetPackage);
                mockRemoteRepository.AddPackage(new MockPackage { Version = new SemanticVersion("3.0") });

                var mockPackageManager = MockGenerator.CreateMockPackageManager(mockRemoteRepository, mockLocalRepository);
                var mockProjectManager = MockGenerator.CreateMockProjectManager(mockRemoteRepository, mockLocalRepository);
                mockProjectManager.Setup(pm => pm.UpdatePackageReference(targetPackage.Id, targetPackage.Version, It.IsAny<bool>(), It.IsAny<bool>())).Verifiable();

                Dependencies.NuGetPackageManagerFactory = MockGenerator.CreatePackageManagerFactory(mockPackageManager.Object);
                Dependencies.NuGetProjectManagerFactory = MockGenerator.CreateProjectManagerFactory(mockProjectManager.Object);
                Dependencies.NuGetProjectSystemFactory = MockGenerator.CreateProjectSystemFactory();

                // act
                NuGetCore core = new NuGetCore();
                var warnings = core.UpdatePackage(@"C:\DummyPath", MockPackage.DefaultId, "http://dummyFeed", "2.0");

                // assert
                mockProjectManager.Verify();
            }

            [Fact]
            public void PackageAvailableFromLocal_UpdateSucceeds()
            {
                // arrange
                var targetPackage = new MockPackage() { Version = new SemanticVersion("2.0") };
                var mockLocalRepository = new MockPackageRepository();
                var mockRemoteRepository = new MockPackageRepository();
                mockLocalRepository.AddPackage(new MockPackage());
                mockLocalRepository.AddPackage(targetPackage);

                var mockPackageManager = MockGenerator.CreateMockPackageManager(mockRemoteRepository, mockLocalRepository);
                var mockProjectManager = MockGenerator.CreateMockProjectManager(mockRemoteRepository, mockLocalRepository);
                mockProjectManager.Setup(pm => pm.UpdatePackageReference(targetPackage.Id, targetPackage.Version, It.IsAny<bool>(), It.IsAny<bool>())).Verifiable();

                Dependencies.NuGetPackageManagerFactory = MockGenerator.CreatePackageManagerFactory(mockPackageManager.Object);
                Dependencies.NuGetProjectManagerFactory = MockGenerator.CreateProjectManagerFactory(mockProjectManager.Object);
                Dependencies.NuGetProjectSystemFactory = MockGenerator.CreateProjectSystemFactory();

                // act
                NuGetCore core = new NuGetCore();
                var warnings = core.UpdatePackage(@"C:\DummyPath", MockPackage.DefaultId, "http://dummyFeed", "2.0");

                // assert
                mockProjectManager.Verify();
            }

            [Fact]
            public void PackageNotFound_ThrowsException()
            {
                // arrange
                var mockLocalRepository = new MockPackageRepository();
                var mockRemoteRepository = new MockPackageRepository();

                var mockPackageManager = new Mock<IPackageManager>();
                var mockProjectManager = MockGenerator.CreateMockProjectManager(mockRemoteRepository, mockLocalRepository);

                Dependencies.NuGetPackageManagerFactory = MockGenerator.CreatePackageManagerFactory(mockPackageManager.Object);
                Dependencies.NuGetProjectManagerFactory = MockGenerator.CreateProjectManagerFactory(mockProjectManager.Object);
                Dependencies.NuGetProjectSystemFactory = MockGenerator.CreateProjectSystemFactory();

                string dummyPackage = "DummyPackage";
                string dummyVersion = "1.0";

                // act
                // assert
                NuGetCore core = new NuGetCore();
                var ex = Assert.Throws<Exception>(() => core.UpdatePackage(@"C:\DummyPath", dummyPackage, "", dummyVersion));
                Assert.Contains(string.Format("No package named {0}.{1} found at location", dummyPackage, dummyVersion), ex.Message);
            }
        }
    }
}
