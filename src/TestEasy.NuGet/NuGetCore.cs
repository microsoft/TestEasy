using System;
using System.Collections.Generic;
using NuGet;
using TestEasy.Core.Abstractions;

namespace TestEasy.NuGet
{
    internal class NuGetCore : INuGetCore
    {
        public IEnumerable<string> InstallPackage(string sitePhysicalPath, string packageName, string source, string version, Version targetFramework = null)
        {
            IPackage package;
            var nugetProject = TryToLocatePackage(sitePhysicalPath, packageName, source, version, targetFramework, out package);

            if (nugetProject == null || package == null)
            {
                throw new Exception(string.Format("No package named {0}.{1} found at location {2}",
                    packageName, (version ?? ""), source));
            }

            return nugetProject.InstallPackage(package);
        }

        public IEnumerable<string> UpdatePackage(string sitePhysicalPath, string packageName, string source, string version, Version targetFramework = null)
        {
            IPackage package;
            var nugetProject = TryToLocatePackage(sitePhysicalPath, packageName, source, version, targetFramework, out package);

            if (nugetProject == null || package == null)
            {
                throw new Exception(string.Format("No package named {0}.{1} found at location {2}",
                    packageName, (version ?? ""), source));
            }

            return nugetProject.UpdatePackage(package);
        }

        private WebNuGetProject TryToLocatePackage(string sitePhysicalPath, string packageName, string source,
                                                     string version, Version targetFramework, out IPackage package)
        {
            var semanticVersion = (string.IsNullOrEmpty(version)) ? null : new SemanticVersion(version);
            var sources = source.Split(';');

            var nugetProject = new WebNuGetProject(sources, sitePhysicalPath, targetFramework);

            package = nugetProject.SourceRepository.FindPackage(packageName, semanticVersion, true, false) ??
                      nugetProject.LocalRepository.FindPackage(packageName, semanticVersion, true, false);

            return package == null ? null : nugetProject;
        }
    }
}
