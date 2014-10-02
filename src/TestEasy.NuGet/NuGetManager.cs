using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using NuGet;
using TestEasy.Core.Abstractions;

namespace TestEasy.NuGet
{
    /// <summary>
    ///     Provides helper APIs to install nuget packages to web sites at runtime
    /// </summary>
    public class NuGetManager
    {
        public const string NUGET_DEFAULT_SOURCE = @"https://nuget.org/api/v2/";
        private readonly INuGetCore _nugetCore;
        private readonly Core.Abstractions.IFileSystem _fileSystem;

        /// <summary>
        ///     ctor
        /// </summary>
        public NuGetManager()
            : this(Dependencies.NuGetCore, Dependencies.FileSystem)
        {
        }

        internal NuGetManager(INuGetCore nugetCore, Core.Abstractions.IFileSystem fileSystem)
        {
            _nugetCore = nugetCore;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Install all of the packages from the specified packages.config file
        /// </summary>
        /// <param name="binariesOnly">Only install binaries (do not modify web.config)</param>
        public IEnumerable<string> InstallPackages(string sitePhysicalPath, string packagesConfigPath, string source = "",
                            bool binariesOnly = true, Version targetFramework = null)
        {
            if (string.IsNullOrEmpty(packagesConfigPath))
            {
                throw new ArgumentNullException("packagesConfig");
            }

            // parse config and get all packages
            if (!_fileSystem.FileExists(packagesConfigPath))
            {
                throw new FileNotFoundException(string.Format("Packages config was not found at: '{0}'.", packagesConfigPath));
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(_fileSystem.FileReadAllText(packagesConfigPath));

            List<PackageName> packages = new List<PackageName>();
            XmlNodeList xmlPackages = doc.GetElementsByTagName("package");
            foreach (XmlNode p in xmlPackages)
            {
                PackageName package = new PackageName(p.Attributes["id"].Value, new SemanticVersion(p.Attributes["version"].Value));

                packages.Add(package);

                if (p.Attributes["source"] != null && !string.IsNullOrEmpty(p.Attributes["source"].Value))
                {
                    source += (";" + p.Attributes["source"].Value);
                }
            }

            return InstallPackages(sitePhysicalPath, packages, source.Trim(';'), binariesOnly, targetFramework);
        }

        /// <summary>
        /// Install the specified set of packages
        /// </summary>
        /// <param name="binariesOnly">Only install binaries (do not modify web.config)</param>
        public IEnumerable<string> InstallPackages(string sitePhysicalPath, IEnumerable<PackageName> packages, string source = "",
                            bool binariesOnly = true, Version targetFramework = null)
        {
            if (packages == null)
            {
                throw new ArgumentNullException("packages");
            }

            List<string> warnings = new List<string>();
            foreach (PackageName p in packages)
            {
                warnings.AddRange(InstallPackage(sitePhysicalPath, p.Id, source, 
                    (p.Version == null ? "" : p.Version.ToString()), binariesOnly, targetFramework));
            }

            return warnings;
        }

        /// <summary>
        /// Install the specified package
        /// </summary>
        /// <param name="binariesOnly">Only install binaries (do not modify web.config)</param>
        public IEnumerable<string> InstallPackage(string appPhysicalPath, string packageName, string source = "",
                            string version = "", bool binariesOnly = true, Version targetFramework = null)
        {
            if (string.IsNullOrEmpty(appPhysicalPath))
            {
                throw new ArgumentNullException("appPhysicalPath");
            }

            if (string.IsNullOrEmpty(packageName))
            {
                throw new ArgumentNullException("packageName");
            }

            if (string.IsNullOrEmpty(source))
            {
                source = NUGET_DEFAULT_SOURCE;
            }
            else
            {
                source += (";" + NUGET_DEFAULT_SOURCE);
            }

            IEnumerable<string> warnings = PerformNugetAction(appPhysicalPath, binariesOnly,
                () => { return _nugetCore.InstallPackage(appPhysicalPath, packageName, source, version, targetFramework); });

            return warnings;
        }

        /// <summary>
        /// Update all of the packages in the specified list
        /// </summary>
        /// <param name="binariesOnly">Only install binaries (do not modify web.config)</param>
        public IEnumerable<string> UpdatePackages(string sitePhysicalPath, IEnumerable<PackageName> packages, string source = "",
                    bool binariesOnly = true, Version targetFramework = null)
        {
            if (packages == null)
            {
                throw new ArgumentNullException("packages");
            }

            List<string> warnings = new List<string>();
            foreach (PackageName p in packages)
            {
                warnings.AddRange(UpdatePackage(sitePhysicalPath, p.Id, source,
                    (p.Version == null ? "" : p.Version.ToString()), binariesOnly, targetFramework));
            }

            return warnings;
        }

        /// <summary>
        /// Update the installed version of the specified package
        /// </summary>
        /// <param name="binariesOnly">Only install binaries (do not modify web.config)</param>
        public IEnumerable<string> UpdatePackage(string appPhysicalPath, string packageName, string source = "", string version = "",
                    bool binariesOnly = true, Version targetFramework = null)
        {
            if (string.IsNullOrEmpty(appPhysicalPath))
            {
                throw new ArgumentNullException("appPhysicalPath");
            }
            if (string.IsNullOrEmpty(packageName))
            {
                throw new ArgumentNullException("packageName");
            }
            if (string.IsNullOrEmpty(source))
            {
                source = NUGET_DEFAULT_SOURCE;
            }
            else
            {
                source += (";" + NUGET_DEFAULT_SOURCE);
            }

            var warnings = PerformNugetAction(appPhysicalPath, binariesOnly, 
                () => { return _nugetCore.UpdatePackage(appPhysicalPath, packageName, source, version, targetFramework); });

            return warnings;
        }

        /// <summary>
        /// Executes an action through Nuget, including making sure directory is writable and handling web.config preservation
        /// </summary>
        /// <param name="binariesOnly">Whether to restore web.config contents after installing packages</param>
        private IEnumerable<string> PerformNugetAction(string appPhysicalPath, bool binariesOnly, Func<IEnumerable<string>> nugetAction)
        {
            // make app folder writable
            _fileSystem.DirectorySetAttribute(appPhysicalPath, FileAttributes.Normal);

            string webConfigPath = Path.Combine(appPhysicalPath, "web.config");
            string webConfigPathTemp = Path.Combine(appPhysicalPath, "web.config.temp");

            if (binariesOnly && _fileSystem.FileExists(webConfigPath))
            {
                // backup web.config                
                _fileSystem.FileCopy(webConfigPath, webConfigPathTemp, true);
            }

            IEnumerable<string> warnings = nugetAction();

            if (binariesOnly && _fileSystem.FileExists(webConfigPathTemp))
            {
                // restore web.config
                _fileSystem.FileCopy(webConfigPathTemp, webConfigPath, true);
                _fileSystem.FileDelete(webConfigPathTemp);
            }

            return warnings;
        }
    }
}
