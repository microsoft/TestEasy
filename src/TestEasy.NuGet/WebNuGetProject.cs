using NuGet;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace TestEasy.NuGet
{
    internal class WebNuGetProject
    {
        private readonly IProjectManager _projectManager;
        private readonly IPackageManager _packageManager;
        private readonly IProjectSystem _projectSystem;

        public WebNuGetProject(IEnumerable<string> remoteSources, string siteRoot, Version targetFramework)
            : this(remoteSources, siteRoot, targetFramework, Dependencies.NuGetProjectSystemFactory)
        {
        }

        internal WebNuGetProject(IEnumerable<string> remoteSources, string siteRoot, Version targetFramework, INuGetProjectSystemFactory projectSystemFactory)
            : this(remoteSources, siteRoot, targetFramework, projectSystemFactory, Dependencies.NuGetPackageManagerFactory, Dependencies.NuGetProjectManagerFactory)
        {
        }

        internal WebNuGetProject(IEnumerable<string> remoteSources, string siteRoot, Version targetFramework, INuGetProjectSystemFactory projectSystemFactory, INuGetPackageManagerFactory packageManagerFactory, INuGetProjectManagerFactory projectManagerFactory)
        {
            _projectSystem = projectSystemFactory.CreateProject(siteRoot);

            // websites don't always know their target framework
            if (_projectSystem is NuGetWebProjectSystem && targetFramework != null)
            {
                ((NuGetWebProjectSystem)_projectSystem).TargetFramework = new FrameworkName(".NetFramework", targetFramework);
            }

            string webRepositoryDirectory = GetWebRepositoryDirectory(siteRoot);

            var enumerable = remoteSources as IList<string> ?? remoteSources.ToList();
            _packageManager = packageManagerFactory.CreatePackageManager(enumerable, webRepositoryDirectory);
            _projectManager = projectManagerFactory.CreateProjectManager(enumerable, webRepositoryDirectory, _projectSystem);
        }

        public IPackageRepository LocalRepository
        {
            get
            {
                return _projectManager.LocalRepository;
            }
        }

        public IPackageRepository SourceRepository
        {
            get
            {
                return _projectManager.SourceRepository;
            }
        }

        public virtual IQueryable<IPackage> GetRemotePackages(string searchTerms)
        {
            var packages = GetPackages(SourceRepository, searchTerms);

            // Order by download count and Id to allow collapsing 
            return packages.OrderByDescending(p => p.DownloadCount).ThenBy(p => p.Id);
        }

        public IQueryable<IPackage> GetInstalledPackages(string searchTerms)
        {
            return GetPackages(LocalRepository, searchTerms);
        }

        [ExcludeFromCodeCoverage] // Depends on repository implementation
        public IEnumerable<IPackage> GetPackagesWithUpdates(string searchTerms)
        {
            var packagesToUpdate = GetPackages(LocalRepository, searchTerms);

            return SourceRepository.GetUpdates(packagesToUpdate, includePrerelease: true, includeAllVersions: true).AsQueryable();
        }

        public IEnumerable<string> InstallPackage(IPackage package)
        {
            return PerformLoggedAction(() =>
            {
                _packageManager.InstallPackage(package, ignoreDependencies: false, allowPrereleaseVersions: true);
                _projectManager.AddPackageReference(package, ignoreDependencies: false, allowPrereleaseVersions: true);
                if (_projectSystem.IsBindingRedirectSupported)
                {
                    AddBindingRedirects(); 
                }
            });
        }

        public IEnumerable<string> UpdatePackage(IPackage package)
        {
            return PerformLoggedAction(() =>
            {
                var preInstalledPackage = _packageManager.LocalRepository.FindPackage(package.Id);
                _packageManager.InstallPackage(package, ignoreDependencies: false, allowPrereleaseVersions: true);
                _projectManager.UpdatePackageReference(package.Id, package.Version, updateDependencies: true, allowPrereleaseVersions: true);
                try
                {
                    _packageManager.UninstallPackage(preInstalledPackage, forceRemove: false, removeDependencies: true);
                }
                catch (InvalidOperationException ex)
                {
                    // most likely, this is from a dependency that still requires the older package
                    _packageManager.Logger.Log(MessageLevel.Warning, "Package {0}.{1} could not be uninstalled: {2}", preInstalledPackage.Id, preInstalledPackage.Version, ex.Message);
                }

                if (_projectSystem.IsBindingRedirectSupported)
                {
                    AddBindingRedirects(); 
                }
            });
        }

        public IEnumerable<string> UninstallPackage(IPackage package, bool removeDependencies)
        {
            return PerformLoggedAction(() =>
            {
                _projectManager.RemovePackageReference(package, forceRemove: false, removeDependencies: removeDependencies);
                _packageManager.UninstallPackage(package, forceRemove: false, removeDependencies: true);
            });
        }

        [ExcludeFromCodeCoverage] // Depends on repository implementation
        public bool IsPackageInstalled(IPackage package)
        {
            return LocalRepository.Exists(package);
        }

        [ExcludeFromCodeCoverage] // Depends on repository implementation
        public IPackage GetUpdate(IPackage package)
        {
            return SourceRepository.GetUpdates(new[] { package }, includePrerelease: true, includeAllVersions: true).SingleOrDefault();
        }

        [ExcludeFromCodeCoverage] // depends on Nuget class implementations
        private void AddBindingRedirects()
        {
            IEnumerable<global::NuGet.Runtime.AssemblyBinding> assemblyBindings;
            AppDomain domain = AppDomain.CreateDomain("referencesDomain", null, Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "", false);
            try
            {
                if(_projectSystem is NuGetMsBuildProjectSystem)
                {
                    var referencePaths = ((NuGetMsBuildProjectSystem)_projectSystem).GetAssemblyReferencePaths();
                    assemblyBindings = global::NuGet.Runtime.BindingRedirectResolver.GetBindingRedirects(referencePaths, domain);
                }
                else
                {
                    // use all binaries in the bin folder
                    assemblyBindings = global::NuGet.Runtime.BindingRedirectResolver.GetBindingRedirects(Path.Combine(_projectSystem.Root, "bin"), domain);
                }

                string configFile = _projectSystem.FileExistsInProject("web.config") ? "web.config" : "app.config";
                global::NuGet.Runtime.BindingRedirectManager bindingManager = new global::NuGet.Runtime.BindingRedirectManager(_projectSystem, configFile);
                bindingManager.AddBindingRedirects(assemblyBindings);
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }


        private IEnumerable<string> PerformLoggedAction(Action action)
        {
            ErrorLogger logger = new ErrorLogger();
            _projectManager.Logger = logger;
            _packageManager.Logger = logger;

            try
            {
                action();
            }
            finally
            {
                _projectManager.Logger = null;
                _packageManager.Logger = null;
            }
            return logger.Errors;
        }

        [ExcludeFromCodeCoverage] // Depends on repository implementation
        internal static IQueryable<IPackage> GetPackages(IPackageRepository repository, string searchTerm)
        {
            return GetPackages(repository.GetPackages(), searchTerm);
        }

        internal static IQueryable<IPackage> GetPackages(IQueryable<IPackage> packages, string searchTerm)
        {
            if (!String.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                packages = packages.Find(searchTerm);
            }
            return packages;
        }

        internal static string GetWebRepositoryDirectory(string siteRoot)
        {
            return Path.Combine(siteRoot, "packages");
        }

        internal class ErrorLogger : ILogger
        {
            private readonly IList<string> _errors = new List<string>();

            public IEnumerable<string> Errors
            {
                get
                {
                    return _errors;
                }
            }

            public void Log(MessageLevel level, string message, params object[] args)
            {
                if (level == MessageLevel.Warning)
                {
                    _errors.Add(String.Format(CultureInfo.CurrentCulture, message, args));
                }
            }

            public FileConflictResolution ResolveFileConflict(string message)
            {
                // always overwrite in the event of a file conflict.
                return FileConflictResolution.OverwriteAll;
            }
        }
    }
}
