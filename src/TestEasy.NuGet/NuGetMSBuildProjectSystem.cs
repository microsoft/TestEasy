using NuGet;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using TestEasyFileSystem = TestEasy.Core.Abstractions.IFileSystem;

namespace TestEasy.NuGet
{
    class NuGetMsBuildProjectSystem : PhysicalFileSystem, IProjectSystem
    {
        IMSBuildProject project;

        public NuGetMsBuildProjectSystem(string projectFilePath)
            : this(projectFilePath, Dependencies.MSBuildProjectFactory)
        {
        }

        internal NuGetMsBuildProjectSystem(string projectFilePath, IMSBuildProjectFactory msbuildProjectFactory)
            : base(Path.GetDirectoryName(projectFilePath))
        {
            project = msbuildProjectFactory.CreateProject(projectFilePath);
        }

        public void AddFrameworkReference(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            project.AddReference(name);
            project.Save();
        }

        public void AddReference(string referencePath, Stream stream)
        {
            if(string.IsNullOrEmpty(referencePath))
            {
                throw new ArgumentNullException("referencePath");
            }

            string projectDirName = Path.GetFileName(Root);
            var hintPath = referencePath;
            if (referencePath.StartsWith(projectDirName, StringComparison.OrdinalIgnoreCase))
            {
                // we end up with this.Root == C:\Foo and referencePath == Foo\packages\Bar.dll
                // so we make an absolute path, then make it into a relative path to get rid of the redundant Foo
                referencePath = PathUtility.GetAbsolutePath(Root, referencePath);
            }
            hintPath = PathUtility.GetRelativePath(PathUtility.EnsureTrailingSlash(Root), referencePath);

            project.AddReference(Path.GetFileNameWithoutExtension(referencePath), hintPath);
            project.Save();
        }

        public bool ReferenceExists(string name)
        {
            if(string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            name = GetReferenceName(name);

            return project.ReferenceExists(name);
        }

        private static string GetReferenceName(string name)
        {
            string extension = Path.GetExtension(name);
            if (extension == ".dll" || extension == ".exe")
            {
                name = Path.GetFileNameWithoutExtension(name);
            }
            return name;
        }

        public void RemoveReference(string name)
        {
            string referenceName = GetReferenceName(name);
            if (ReferenceExists(referenceName))
            {
                project.RemoveItem("Reference", referenceName);
                project.Save();
            }
        }

        [ExcludeFromCodeCoverage] // delegated to the MSBuild project
        public bool FileExistsInProject(string path)
        {
            return project.AnyItemExists(path);
        }

        // TODO: implement support for import files later.  We don't need this now as it's a niche case not used by our packages.
        public void AddImport(string targetPath, ProjectImportLocation location)
        {
            throw new NotImplementedException();
        }

        public void RemoveImport(string targetPath)
        {
            throw new NotImplementedException();
        }

        public bool IsBindingRedirectSupported
        {
            get { return true; }
        }

        public bool IsSupportedFile(string path)
        {
            // all file types are supported
            return true;
        }

        public string ProjectName
        {
            get { return Path.GetFileNameWithoutExtension(project.FullPath); }
        }

        public string ResolvePath(string path)
        {
            // no modification to paths
            return path;
        }

        [ExcludeFromCodeCoverage] // delegated to the MSBuild project
        public FrameworkName TargetFramework
        {
            get
            {
                return new FrameworkName(GetPropertyValue("TargetFrameworkMoniker"));
            }
        }

        [ExcludeFromCodeCoverage] // delegated to the MSBuild project
        public dynamic GetPropertyValue(string propertyName)
        {
            return project.GetPropertyValue(propertyName);
        }
        
        internal IEnumerable<string> GetAssemblyReferencePaths()
        {
            Core.Abstractions.IFileSystem fileSystem = Dependencies.FileSystem;

            HashSet<string> referenceList = new HashSet<string>();

            var references = project.GetItemsWithMetadataProperty("Reference", "HintPath");
            foreach (var name in references.Keys)
            {
                var assemblyPath = references[name];
                if (!Path.IsPathRooted(assemblyPath))
                {
                    // make sure these are absolute paths, as anybody other than this project won't know the root.
                    assemblyPath = PathUtility.GetAbsolutePath(PathUtility.EnsureTrailingSlash(Root), assemblyPath);
                }
                if (fileSystem.FileExists(assemblyPath))
                {
                    referenceList.Add(assemblyPath);
                }
            }

            return referenceList;
        }
    }
}
