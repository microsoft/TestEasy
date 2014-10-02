using NuGet;
using System.Collections.Generic;

namespace TestEasy.NuGet
{
    public interface INuGetProjectManagerFactory
    {
        IProjectManager CreateProjectManager(IEnumerable<string> remoteSources, string packagesPath, IProjectSystem project);
    }
}
