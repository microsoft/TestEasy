using NuGet;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using TestEasy.Core.Helpers;
using TestEasyAbstractions = TestEasy.Core.Abstractions;

namespace TestEasy.NuGet
{
    internal class NuGetWebProjectSystem : PhysicalFileSystem, IProjectSystem
    {
        #region static helper methods

        internal static void AddReferencesToConfig(IFileSystem fileSystem, string referenceName)
        {
            var webConfigPath = Path.Combine(fileSystem.Root, "web.config");

            XmlHelper xmlHelper = new XmlHelper();
            string assemblyXpath = String.Format(@"/configuration/system.web/compilation/assemblies/add[@assembly=""{0}""]", referenceName);

            string document;
            bool existingAssembly = false;
            if (fileSystem.FileExists(webConfigPath))
            {
                document = ReadDocument(fileSystem, webConfigPath);
                existingAssembly = xmlHelper.XmlContainsElement(document, assemblyXpath);
            }
            else
            {
                document = "<configuration />";
            }

            if (!existingAssembly)
            {
                var newDocument = String.Format(@"<configuration>
    <system.web>
        <compilation>
            <assemblies>
                <add assembly=""{0}"" />
            </assemblies>
        </compilation>
    </system.web>
</configuration>", referenceName);

                string combinedConfig = xmlHelper.MergeXml(newDocument, document);

                SaveDocument(fileSystem, webConfigPath, combinedConfig);
            }
        }

        private static string ReadDocument(IFileSystem fileSystem, string path)
        {
            string document;
            using (Stream stream = fileSystem.OpenFile(path))
            using (StreamReader sr = new StreamReader(stream))
            {
                document = sr.ReadToEnd();
            }
            return document;
        }

        private static void SaveDocument(IFileSystem fileSystem, string path, string content)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(stream))
                {
                    sw.Write(content);
                    sw.Flush();
                    stream.Seek(0, SeekOrigin.Begin);
                    fileSystem.AddFile(path, stream);
                }
            }
        }

        private static bool RequiresAppCodeRemapping(string path)
        {
            return !IsUnderStandardCodeFolder(path) && IsSourceFile(path) && !IsCodeBehindFile(path);
        }

        private static bool IsUnderAppCode(string path)
        {
            return path.StartsWith(AppCodeFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUnderStandardCodeFolder(string path)
        {
            return _standardAspNetCodeFolders.Where(
                a => path.StartsWith(a + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)).Count() > 0;
        }

        private static bool IsSourceFile(string path)
        {
            return _sourceFileExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsCodeBehindFile(string path)
        {
            string extension = Path.GetExtension(path);

            return _sourceFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
                && _codeBehindFileExtensions.Where(e => path.EndsWith(e + extension, StringComparison.OrdinalIgnoreCase)).Count() > 0;
        }

        #endregion

        private const string BinDir = "bin";
        private const string AppCodeFolder = "App_Code";
        private static readonly string[] _standardAspNetCodeFolders = { "App_Code", "App_Start" };

        private static readonly string[] _generatedFilesFolder = { "Generated___Files" };
        private static readonly string[] _codeBehindFileExtensions = { ".aspx", ".ascx", ".designer", ".master" };
        private static readonly string[] _sourceFileExtensions = { ".cs", ".vb" };

        private TestEasyAbstractions.IFileSystem _fileSystem;
        private TestEasyAbstractions.IAssemblyResolver _assemblyResolver;

        public NuGetWebProjectSystem(string root)
            : this(root, Dependencies.FileSystem, Dependencies.AssemblyResolver)
        {
        }

        internal NuGetWebProjectSystem(string root, TestEasyAbstractions.IFileSystem fileSystem, TestEasyAbstractions.IAssemblyResolver assemblyResolver)
            : base(root)
        {
            _fileSystem = fileSystem;
            _assemblyResolver = assemblyResolver;
        }

        protected virtual string GetReferencePath(string name)
        {
            return Path.Combine(BinDir, name);
        }

        protected string GetAbsolutePath(string basePath, string relativePath)
        {
            if (basePath == null)
            {
                throw new ArgumentNullException("basePath");
            }

            if (relativePath == null)
            {
                throw new ArgumentNullException("relativePath");
            }

            Uri resultUri = new Uri(new Uri(basePath), new Uri(relativePath, UriKind.Relative));
            return resultUri.LocalPath;
        }

        #region IProjectSystem

        [ExcludeFromCodeCoverage] // Depends on physical file system
        public void AddReference(string referencePath, Stream stream)
        {
            if (String.IsNullOrEmpty(referencePath))
            {
                throw new ArgumentNullException("referencePath");
            }

            Dictionary<string, string> filesToCopy = GetReferenceFilesToAdd(referencePath);
            foreach (var file in filesToCopy)
            {
                using (Stream s = _fileSystem.FileOpenRead(file.Key))
                {
                    AddFile(file.Value, s);
                }
            }
        }

        internal Dictionary<string, string> GetReferenceFilesToAdd(string referencePath)
        {
            Dictionary<string, string> result = new Dictionary<string,string>();

            string source = GetAbsolutePath(Root, referencePath);
            if (!_fileSystem.FileExists(source))
            {
                throw new FileNotFoundException(source);
            }

            // Copy to bin by default
            string referenceName = Path.GetFileName(referencePath);
            string dest = GetFullPath(GetReferencePath(referenceName));

            result.Add(source, dest);

            string sourceDirectory = Path.GetDirectoryName(source);
            string destDirectory = Path.GetDirectoryName(dest);

            foreach (var dir in _fileSystem.DirectoryGetSubDirs(sourceDirectory))
            {
                string culture = Path.GetFileName(dir);
                foreach (var file in _fileSystem.DirectoryGetFiles(dir))
                {
                    dest = Path.Combine(destDirectory, culture, Path.GetFileName(file));
                    result.Add(file, dest);
                }
            }

            return result;
        }

        public bool IsSupportedFile(string path)
        {
            return !(Path.GetFileName(path).StartsWith("app.", StringComparison.OrdinalIgnoreCase) && Path.GetFileName(path).EndsWith(".config", StringComparison.OrdinalIgnoreCase));
        }

        public string ProjectName
        {
            get { return Root; }
        }

        public bool IsBindingRedirectSupported
        {
            get
            {
                return true;
            }
        }

        [ExcludeFromCodeCoverage] // Depends on base class
        public bool ReferenceExists(string name)
        {
            string path = GetReferencePath(name);
            return FileExists(path);
        }

        [ExcludeFromCodeCoverage] // depends on base class
        public void RemoveReference(string name)
        {
            DeleteFile(GetReferencePath(name));

            // Delete the bin directory if this was the last reference
            if (!GetFiles(BinDir, true).Any())
            {
                DeleteDirectory(BinDir);
            }
        }

        private FrameworkName _targetFramework = VersionUtility.DefaultTargetFramework;
        public FrameworkName TargetFramework
        {
            get { return _targetFramework; }
            set { _targetFramework = value; }
        }

        public void AddFrameworkReference(string name)
        {
            // Before we add a framework assembly to web.config, verify that it exists in the GAC. This is important because a website would be completely unusable if the assembly reference
            // does not exist and is added to web.config. Since the assembly name may be a partial name, We use the ResolveAssemblyReference task in Msbuild to identify a full name and if it is 
            // installed in the GAC. 
            var fullName = _assemblyResolver.ResolvePartialAssemblyName(name);
            if (fullName == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Unknown assembly reference: {0}", name));
            }
            AddReferencesToConfig(this, fullName);
        }

        public string ResolvePath(string path)
        {
            if (RequiresAppCodeRemapping(path))
            {
                path = Path.Combine(AppCodeFolder, path);
            }
            return path;
        }

        [ExcludeFromCodeCoverage] // Depends on base class
        public bool FileExistsInProject(string path)
        {
            return base.FileExists(path);
        }

        public void AddImport(string targetPath, ProjectImportLocation location)
        {
            throw new NotImplementedException("Not supported for websites");
        }

        public void RemoveImport(string targetPath)
        {
            throw new NotImplementedException("Not supported for websites");
        }

        #endregion

        #region IPropertyProvider

        public dynamic GetPropertyValue(string propertyName)
        {
            if (propertyName == null)
            {
                return null;
            }

            // Return empty string for the root namespace of this project.
            if (propertyName.Equals("RootNamespace", StringComparison.OrdinalIgnoreCase))
            {
                return String.Empty;
            }

            return null;
        }

        #endregion

        #region PhysicalFileSystem

        [ExcludeFromCodeCoverage] // Depends on base class
        public override IEnumerable<string> GetDirectories(string path)
        {
            if (IsUnderAppCode(path))
            {
                // There is an invisible folder called Generated___Files under app code that we want to exclude from our search
                return base.GetDirectories(path).Except(_generatedFilesFolder, StringComparer.OrdinalIgnoreCase);
            }
            return base.GetDirectories(path);
        }

        #endregion

    }
}
