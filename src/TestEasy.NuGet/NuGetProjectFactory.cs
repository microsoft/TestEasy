using NuGet;
using System;
using System.IO;
using System.Linq;
using TestEasy.Core;

namespace TestEasy.NuGet
{
    internal class NuGetProjectFactory : INuGetProjectSystemFactory
    {
        private Core.Abstractions.IFileSystem FileSystem { get; set; }
        private IMSBuildProjectFactory MsBuildProjectFactory { get; set; }

        public NuGetProjectFactory()
            : this(Dependencies.FileSystem, Dependencies.MSBuildProjectFactory)
        {
        }

        internal NuGetProjectFactory(Core.Abstractions.IFileSystem fileSystem)
            : this(fileSystem, Dependencies.MSBuildProjectFactory)
        {
        }

        internal NuGetProjectFactory(Core.Abstractions.IFileSystem fileSystem, IMSBuildProjectFactory msbuildFactory)
        {
            FileSystem = fileSystem;
            MsBuildProjectFactory = msbuildFactory;
        }

        public IProjectSystem CreateProject(string siteRoot)
        {
            var projectFiles = FileSystem.DirectoryGetFiles(siteRoot).Where(file => Path.GetExtension(file) == ".csproj" || Path.GetExtension(file) == ".vbproj").ToList();
            if(projectFiles.Any())
            {
                var projectFile = projectFiles.First();
                try
                {
                    return new NuGetMsBuildProjectSystem(projectFile, MsBuildProjectFactory);
                }
                catch(Microsoft.Build.Exceptions.InvalidProjectFileException)
                {
                    TestEasyLog.Instance.Warning(String.Format("TestEasy.Nuget could not open MSBuild project [{0}].  Falling back to Website project type instead.", projectFile));
                }
            }

            return new NuGetWebProjectSystem(siteRoot);
        }
    }
}
