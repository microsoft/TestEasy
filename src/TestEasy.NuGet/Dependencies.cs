using System.Diagnostics.CodeAnalysis;
using TestEasy.Core.Abstractions;

namespace TestEasy.NuGet
{
    [ExcludeFromCodeCoverage]
    internal static class Dependencies
    {
        private static IAssemblyResolver _assemblyResolver;
        public static IAssemblyResolver AssemblyResolver
        {
            get { return _assemblyResolver ?? (_assemblyResolver = new AssemblyResolver()); }
            set { _assemblyResolver = value; }
        }

        private static IFileSystem _fileSystem;
        public static IFileSystem FileSystem
        {
            get { return _fileSystem ?? (_fileSystem = new FileSystem()); }
            set
            {
                _fileSystem = value;
            }
        }

        private static IMSBuildProjectFactory _msbuildProjectFactory;
        public static IMSBuildProjectFactory MSBuildProjectFactory
        {
            get
            {
                return _msbuildProjectFactory ?? (_msbuildProjectFactory = new MSBuildProjectFactory());
            }
            set
            {
                _msbuildProjectFactory = value;
            }
        }

        private static INuGetCore _nuGetCore;
        public static INuGetCore NuGetCore
        {
            get { return _nuGetCore ?? (_nuGetCore = new NuGetCore()); }
            set { _nuGetCore = value; }
        }

        private static INuGetPackageManagerFactory _nuGetManagerFactory;
        internal static INuGetPackageManagerFactory NuGetPackageManagerFactory
        {
            get { return _nuGetManagerFactory ?? (_nuGetManagerFactory = new NuGetManagerFactory()); }
            set { _nuGetManagerFactory = value; }
        }

        private static INuGetProjectManagerFactory _nuGetProjectManagerFactory;
        internal static INuGetProjectManagerFactory NuGetProjectManagerFactory
        {
            get { return _nuGetProjectManagerFactory ?? (_nuGetProjectManagerFactory = new NuGetManagerFactory()); }
            set { _nuGetProjectManagerFactory = value; }
        }

        private static INuGetProjectSystemFactory _nuGetProjectSystemFactory;
        internal static INuGetProjectSystemFactory NuGetProjectSystemFactory
        {
            get { return _nuGetProjectSystemFactory ?? (_nuGetProjectSystemFactory = new NuGetProjectFactory()); }
            set { _nuGetProjectSystemFactory = value; }
        }
    }
}
