using NuGet;
using System.Collections.Generic;

namespace TestEasy.NuGet
{
    public interface INuGetPackageManagerFactory
    {
        IPackageManager CreatePackageManager(IEnumerable<string> remoteSources, string packagesPath);
    }
}
