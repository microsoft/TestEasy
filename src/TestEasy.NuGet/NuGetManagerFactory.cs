using NuGet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestEasy.NuGet
{
    internal class NuGetManagerFactory : INuGetProjectManagerFactory, INuGetPackageManagerFactory
    {
        internal NuGetManagerFactory()
        {
        }

        public IProjectManager CreateProjectManager(IEnumerable<string> remoteSources, string packagesPath, IProjectSystem project)
        {
            if(remoteSources == null)
            {
                throw new ArgumentNullException("remoteSources");
            }
            if (!remoteSources.Any())
            {
                throw new ArgumentException("Must provide at least one remote source");
            }

            var sourceRepo = new AggregateRepository(PackageRepositoryFactory.Default, remoteSources, true);
            var pathResolver = new DefaultPackagePathResolver(packagesPath);

            var packagesConfigRepo = new PackageReferenceRepository(project, project.ProjectName, new SharedPackageRepository(packagesPath));

            return new ProjectManager(sourceRepository: sourceRepo,
                                        pathResolver: pathResolver,
                                        localRepository: packagesConfigRepo,
                                        project: project);
        }

        public IPackageManager CreatePackageManager(IEnumerable<string> remoteSources, string packagesPath)
        {
            if (remoteSources == null)
            {
                throw new ArgumentNullException("remoteSources");
            }
            if (!remoteSources.Any())
            {
                throw new ArgumentException("Must provide at least one remote source");
            }

            var sourceRepo = new AggregateRepository(PackageRepositoryFactory.Default, remoteSources, true);
            var pathResolver = new DefaultPackagePathResolver(packagesPath);
            var packageManagerFileSystem = new PhysicalFileSystem(packagesPath);

            return new PackageManager(sourceRepository: sourceRepo,
                                        pathResolver: pathResolver,
                                        fileSystem: packageManagerFileSystem);
        }

    }
}
