using Moq;
using NuGet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestEasy.Core.Abstractions;
using TestEasy.NuGet;

namespace TestEasy.Nuget.Test.Mocks
{
    internal static class MockGenerator
    {
        #region IFileSystem

        public static Mock<TestEasy.Core.Abstractions.IFileSystem> CreateMockFileSystem()
        {
            return new Mock<TestEasy.Core.Abstractions.IFileSystem>();
        }

        #endregion

        #region IMSBuildProject

        public static Mock<IMSBuildProject> CreateMockMSBuildProject()
        {
            return new Mock<IMSBuildProject>();
        }

        public static Mock<IMSBuildProject> CreateMockMSBuildProject_SaveMethodVerifiable()
        {
            Mock<IMSBuildProject> mockProject = CreateMockMSBuildProject();
            mockProject.Setup(p => p.Save()).Verifiable();
            return mockProject;
        }

        public static IMSBuildProjectFactory CreateMSBuildProjectFactory(IMSBuildProject project = null)
        {
            if (project == null)
            {
                project = new Mock<IMSBuildProject>() { DefaultValue = DefaultValue.Mock }.Object;
            }

            var factory = new Mock<IMSBuildProjectFactory>();
            factory.Setup(f => f.CreateProject(It.IsAny<string>())).Returns(project);

            return factory.Object;
        }

        #endregion

        #region INuGetProjectSystemFactory

        public static INuGetProjectSystemFactory CreateProjectSystemFactory(IProjectSystem projectSystem = null)
        {
            if(projectSystem == null)
            {
                var mockProjectSystem = new Mock<IProjectSystem>();
                mockProjectSystem.Setup(ps => ps.Root).Returns("");
                projectSystem = mockProjectSystem.Object;
            }

            var mock = new Mock<INuGetProjectSystemFactory>();
            mock.Setup(f => f.CreateProject(It.IsAny<string>())).Returns(projectSystem);
            return mock.Object;
        }

        /// <summary>
        ///  Factory which always creates a NuGetWebProjectSystem for the specified directory.
        /// </summary>
        public static INuGetProjectSystemFactory CreateWebProjectSystemFactory()
        {
            var mock = new Mock<INuGetProjectSystemFactory>();
            mock.Setup(f => f.CreateProject(It.IsAny<string>())).Returns<string>(s => new NuGetWebProjectSystem(s));
            return mock.Object;
        }

        #endregion

        #region IPackageManager
        internal static Mock<IPackageManager> CreateMockPackageManager()
        {
            return CreateMockPackageManager(new MockPackageRepository(), new MockPackageRepository());
        }

        internal static Mock<IPackageManager> CreateMockPackageManager(IPackageRepository remote, IPackageRepository local)
        {
            var mockManager = new Mock<IPackageManager>();
            mockManager.Setup(pm => pm.SourceRepository).Returns(remote);
            mockManager.Setup(pm => pm.LocalRepository).Returns(local);

            return mockManager;
        }

        internal static INuGetPackageManagerFactory CreatePackageManagerFactory(IPackageManager pm)
        {
            var mockFactory = new Mock<INuGetPackageManagerFactory>();
            mockFactory.Setup(f => f.CreatePackageManager(It.IsAny<IEnumerable<string>>(), It.IsAny<string>())).Returns(pm);

            return mockFactory.Object;
        }
        #endregion

        #region IProjectManager
        internal static Mock<IProjectManager> CreateMockProjectManager(IPackageRepository remote, IPackageRepository local)
        {
            var mockManager = new Mock<IProjectManager>();
            mockManager.Setup(pm => pm.SourceRepository).Returns(remote);
            mockManager.Setup(pm => pm.LocalRepository).Returns(local);

            return mockManager;
        }

        internal static INuGetProjectManagerFactory CreateProjectManagerFactory(IProjectManager pm)
        {
            var mockFactory = new Mock<INuGetProjectManagerFactory>();
            mockFactory.Setup(f => f.CreateProjectManager(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<IProjectSystem>())).Returns(pm);

            return mockFactory.Object;
        }

        #endregion
    }
}
