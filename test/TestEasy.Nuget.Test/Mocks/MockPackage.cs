using NuGet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace TestEasy.Nuget.Test.Mocks
{
    class MockPackage : IPackage
    {
        public static readonly string DefaultId = "MockPackage";
        public static readonly SemanticVersion DefaultVersion = new SemanticVersion("1.0");

        public MockPackage()
        {
            Id = DefaultId;
            Version = DefaultVersion;
        }

        public MockPackage(string id, string version)
        {
            Id = id;
            Version = new SemanticVersion(version);
        }

        public string Id
        {
            get;
            set;
        }

        public SemanticVersion Version
        {
            get;
            set;
        }

        public IEnumerable<PackageDependencySet> DependencySets
        {
            get;
            set;
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences
        {
            get { return Enumerable.Empty<IPackageAssemblyReference>(); }
        }

        public IEnumerable<IPackageFile> GetFiles()
        {
            return Enumerable.Empty<IPackageFile>();
        }

        public Stream GetStream()
        {
            return Stream.Null;
        }

        public IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            return Enumerable.Empty<FrameworkName>();
        }

        public bool IsAbsoluteLatestVersion
        {
            get { return true; }
        }

        public bool IsLatestVersion
        {
            get { return true; }
        }

        public bool Listed
        {
            get { return true; }
        }

        public DateTimeOffset? Published
        {
            get { return null; }
        }

        public IEnumerable<string> Authors
        {
            get { return Enumerable.Empty<string>(); }
        }

        public string Copyright
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies
        {
            get { return Enumerable.Empty<FrameworkAssemblyReference>(); }
        }

        public Uri IconUrl
        {
            get { return null; }
        }


        public string Language
        {
            get;
            set;
        }

        public Uri LicenseUrl
        {
            get { return null; }
        }

        public Version MinClientVersion
        {
            get { return null; }
        }

        public IEnumerable<string> Owners
        {
            get { return Enumerable.Empty<string>(); }
        }

        public ICollection<PackageReferenceSet> PackageAssemblyReferences
        {
            get { return null; }
        }

        public Uri ProjectUrl
        {
            get { return null; }
        }

        public string ReleaseNotes
        {
            get;
            set;
        }

        public bool RequireLicenseAcceptance
        {
            get;
            set;
        }

        public string Summary
        {
            get;
            set;
        }

        public string Tags
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }


        public int DownloadCount
        {
            get;
            set;
        }

        public Uri ReportAbuseUrl
        {
            get { return null; }
        }

        public bool DevelopmentDependency
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public class StubPackageComparer : IEqualityComparer<IPackage>
        {
            public bool Equals(IPackage x, IPackage y)
            {
                return x != null
                    && y != null
                    && x.Id == y.Id
                    && x.Version == y.Version;
            }

            public int GetHashCode(IPackage obj)
            {
                throw new NotImplementedException();
            }
        }

    }
}
