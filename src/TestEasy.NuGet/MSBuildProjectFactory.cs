namespace TestEasy.NuGet
{
    class MSBuildProjectFactory : IMSBuildProjectFactory
    {
        public IMSBuildProject CreateProject(string projectFilePath)
        {
            return new MsBuildProject(projectFilePath);
        }
    }
}
