using NuGet;

namespace TestEasy.NuGet
{
    public interface INuGetProjectSystemFactory
    {
        IProjectSystem CreateProject(string siteRoot);
    }
}
