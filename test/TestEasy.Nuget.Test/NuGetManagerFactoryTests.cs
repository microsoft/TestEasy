using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestEasy.NuGet;
using Xunit;

namespace TestEasy.Nuget.Test
{
    public class NuGetManagerFactoryTests
    {
        public class CreateProjectManager
        {
            [Fact]
            public void WhenRemoteSourcesNull_Throws()
            {
                // arrange
                var mockProjectSystem = new Mock<global::NuGet.IProjectSystem>();
                mockProjectSystem.Setup(ps => ps.Root).Returns(@"C:\DummyRoot");
                var pmf = new NuGetManagerFactory();

                // act
                Assert.Throws<ArgumentNullException>(() => pmf.CreateProjectManager(null, "dummy", mockProjectSystem.Object));
            }

            [Fact]
            public void WhenRemoteSourcesEmpty_Throws()
            {
                // arrange
                var mockProjectSystem = new Mock<global::NuGet.IProjectSystem>();
                mockProjectSystem.Setup(ps => ps.Root).Returns(@"C:\DummyRoot");
                var pmf = new NuGetManagerFactory();

                // act
                Assert.Throws<ArgumentException>(() => pmf.CreateProjectManager(Enumerable.Empty<string>(), "dummy", mockProjectSystem.Object));
            }

            [Fact]
            public void LocalRepositoryPathContainsPackages_Config()
            {
                // arrange
                var mockProjectSystem = new Mock<global::NuGet.IProjectSystem>();
                mockProjectSystem.Setup(ps => ps.Root).Returns(@"C:\DummyRoot");
                var dummySourceFeed = new string[] { "http://dummyFeed" };
                var dummyPackagesLocation = @"c:\dummyPackagesPath";
                var pmf = new NuGetManagerFactory();

                // act
                var manager = pmf.CreateProjectManager(dummySourceFeed, dummyPackagesLocation, mockProjectSystem.Object);

                // assert
                // local repository should use a packages.config file
                Assert.Contains("packages.config", manager.LocalRepository.Source);
            }

        }

        public class CreatePackageManager
        {
            [Fact]
            public void WhenRemoteSourcesNull_Throws()
            {
                var pmf = new NuGetManagerFactory();
                Assert.Throws<ArgumentNullException>(() => pmf.CreatePackageManager(null, @"C:\packages"));
            }

            [Fact]
            public void WhenRemoteSourcesEmpty_Throws()
            {
                var pmf = new NuGetManagerFactory();
                Assert.Throws<ArgumentException>(() => pmf.CreatePackageManager(Enumerable.Empty<string>(), @"C:\packages"));
            }

            [Fact]
            public void LocalRepositoryPath()
            {
                var dummySourceFeed = new string[] { "http://dummyFeed" };
                var dummyPackagesLocation = @"C:\dummyPackages";

                var pmf = new NuGetManagerFactory();
                var packMan = pmf.CreatePackageManager(dummySourceFeed, dummyPackagesLocation);


                Assert.Equal(dummyPackagesLocation, packMan.LocalRepository.Source);
            }
        }
    }
}
