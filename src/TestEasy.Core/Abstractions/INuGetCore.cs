using System;
using System.Collections.Generic;

namespace TestEasy.Core.Abstractions
{
    public interface INuGetCore
    {
        IEnumerable<string> InstallPackage(string sitePhysicalPath, string packageName,
                                           string source, string version, Version targetFramework);

        IEnumerable<string> UpdatePackage(string sitePhysicalPath, string packageName,
                                           string source, string version, Version targetFramework);
    }
}
