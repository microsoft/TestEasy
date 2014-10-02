using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet;
using System.Collections.Generic;
using System.IO;
using TestEasy.NuGet;
using TestEasy.WebServer;

namespace SampleMSUnitTests.Tests
{
    [TestClass]
    [DeploymentItem("SampleWebSites")]
    [DeploymentItem("SampleNugetPackages")]
    public class NugetTests
    {
        private WebApplicationInfo _appInfo;

        [TestInitialize]
        public void SetupWebsite()
        {
            // common logic to arrange the test website
            const string webSiteName = "TestEasyWebSite";
            var server = WebServer.Create();
            _appInfo = server.CreateWebApplication("TestEasyWebSite");

            server.DeployWebApplication(_appInfo.Name, new List<DeploymentItem>
                {
                    new DeploymentItem { Type = DeploymentItemType.Directory, Path = webSiteName }
                });
        }

        [TestMethod]
        [TestCategory("BVT")]
        public void InstallSatelliteAssemblyTest()
        {
            // arrange
            NuGetManager nuget = new NuGetManager();
            var nugetSources = AbsolutePath(@"LocalizedNugetPackage");

            // act
            nuget.InstallPackages(
                _appInfo.PhysicalPath,
                new List<PackageName>
                    {
                        new PackageName("LocalizedNugetPackage.zh-Hans", new SemanticVersion("1.0"))
                    },
                nugetSources);

            // assert
            var expectedLocAssembly = Path.Combine(_appInfo.PhysicalPath, @"bin\zh-Hans\LocalizedNugetPackage.resources.dll");
            Assert.IsTrue(File.Exists(expectedLocAssembly));
        }

        [TestMethod]
        [TestCategory("BVT")]
        public void UpdatePackageToSpecificVersion()
        {
            // arrange
            NuGetManager nuget = new NuGetManager();
            var nugetSources = AbsolutePath(@"LocalizedNugetPackage");

            nuget.InstallPackages(
                _appInfo.PhysicalPath,
                new List<PackageName>
                    {
                        new PackageName("LocalizedNugetPackage", new SemanticVersion("1.0"))
                    },
                nugetSources);


            // act
            nuget.UpdatePackages(
                _appInfo.PhysicalPath,
                new List<PackageName>
                    {
                        new PackageName("LocalizedNugetPackage", new SemanticVersion("2.0.0"))
                    },
                nugetSources);

            // assert
            var expectedPackageInstallDir = Path.Combine(_appInfo.PhysicalPath, "packages", "LocalizedNugetPackage.2.0.0");
            Assert.IsTrue(Directory.Exists(expectedPackageInstallDir));
            var notExpectedPackageInstallDir = Path.Combine(_appInfo.PhysicalPath, "packages", "LocalizedNugetPackage.1.0.0");
            Assert.IsFalse(Directory.Exists(notExpectedPackageInstallDir));
        }

        [TestMethod]
        [TestCategory("BVT")]
        public void UpdatePackageToLatestVersion()
        {
            // arrange
            NuGetManager nuget = new NuGetManager();
            var nugetSources = AbsolutePath(@"LocalizedNugetPackage");

            nuget.InstallPackages(
                _appInfo.PhysicalPath,
                new List<PackageName>
                    {
                        new PackageName("LocalizedNugetPackage", new SemanticVersion("1.0"))
                    },
                nugetSources);


            // act
            nuget.UpdatePackage(
                _appInfo.PhysicalPath,
                "LocalizedNugetPackage",
                nugetSources);

            // assert
            var expectedPackageInstallDir = Path.Combine(_appInfo.PhysicalPath, "packages", "LocalizedNugetPackage.3.0.0-prerelease");
            Assert.IsTrue(Directory.Exists(expectedPackageInstallDir));
            var notExpectedPackageInstallDir = Path.Combine(_appInfo.PhysicalPath, "packages", "LocalizedNugetPackage.1.0.0");
            Assert.IsFalse(Directory.Exists(notExpectedPackageInstallDir));
        }

        private string AbsolutePath(string relativePath)
        {
            return Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
                                relativePath);
        }
    }

    [TestClass]
    [DeploymentItem("SampleWebSites")]
    [DeploymentItem("SampleNugetPackages")]
    public class NugetMsBuildTests
    {
        const string WebSiteName = "TestEasyWAP";
        private WebApplicationInfo _appInfo;
        private WebServer _server;

        [TestInitialize]
        public void SetupWebsite()
        {
            // common logic to arrange the test website
            _server = WebServer.Create();
            _appInfo = _server.CreateWebApplication("TestEasyWAP");

            _server.DeployWebApplication(_appInfo.Name, new List<DeploymentItem>
                {
                    new DeploymentItem { Type = DeploymentItemType.Directory, Path = WebSiteName }
                });
        }

        [TestMethod]
        [TestCategory("BVT")]
        public void Build_CopySatelliteAssemblyToOutputDir()
        {
            // arrange
            NuGetManager nuget = new NuGetManager();
            var nugetSources = AbsolutePath(@"LocalizedNugetPackage");

            // act
            nuget.InstallPackages(
                _appInfo.PhysicalPath,
                new List<PackageName>
                    {
                        new PackageName("LocalizedNugetPackage.zh-Hans", new SemanticVersion("1.0"))
                    },
                nugetSources);
            _server.BuildWebApplication(_appInfo.Name, WebSiteName);

            // assert
            var expectedLocAssembly = Path.Combine(_appInfo.PhysicalPath, @"bin\zh-Hans\LocalizedNugetPackage.resources.dll");
            Assert.IsTrue(File.Exists(expectedLocAssembly));
        }

        [TestMethod]
        [TestCategory("BVT")]
        public void UpdatePackage_UpdatesReferencesInProjectFile()
        {
            // arrange
            NuGetManager nuget = new NuGetManager();
            var nugetSources = AbsolutePath(@"LocalizedNugetPackage");

            nuget.InstallPackages(
                _appInfo.PhysicalPath,
                new List<PackageName>
                    {
                        new PackageName("LocalizedNugetPackage", new SemanticVersion("1.0"))
                    },
                nugetSources);


            // act
            nuget.UpdatePackage(
                _appInfo.PhysicalPath,
                "LocalizedNugetPackage",
                nugetSources);

            var projectFileContents = File.ReadAllText(Path.Combine(_appInfo.PhysicalPath, "TestEasyWAP.csproj"));

            // assert
            var expectedPackageInstallDir = Path.Combine(_appInfo.PhysicalPath, "packages", "LocalizedNugetPackage.3.0.0-prerelease");
            Assert.IsTrue(Directory.Exists(expectedPackageInstallDir));
            Assert.IsTrue(projectFileContents.Contains("LocalizedNugetPackage.3.0.0-prerelease"));
            var notExpectedPackageInstallDir = Path.Combine(_appInfo.PhysicalPath, "packages", "LocalizedNugetPackage.1.0.0");
            Assert.IsFalse(Directory.Exists(notExpectedPackageInstallDir));
            Assert.IsFalse(projectFileContents.Contains("LocalizedNugetPackage.1.0.0"));
            
        }

        private string AbsolutePath(string relativePath)
        {
            return Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
                relativePath);
        }
    }
}
