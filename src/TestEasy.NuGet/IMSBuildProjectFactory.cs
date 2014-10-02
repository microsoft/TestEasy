namespace TestEasy.NuGet
{
    public interface IMSBuildProjectFactory
    {
        IMSBuildProject CreateProject(string projectFilePath);
    }
}
