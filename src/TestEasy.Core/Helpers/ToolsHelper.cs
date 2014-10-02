using System;
using System.IO;
using TestEasy.Core.Abstractions;
using TestEasy.Core.Configuration;

namespace TestEasy.Core.Helpers
{
    /// <summary>
    ///     API helping to find and initialize various tools on test machine at runtime
    /// </summary>
    public class ToolsHelper
    {
        internal readonly IFileSystem FileSystem;
        internal readonly IEnvironmentSystem EnvironmentSystem;
        internal readonly IProcessRunner ProcessRunner;

        /// <summary>
        ///     ctor
        /// </summary>
        public ToolsHelper()
            :this(AbstractionsLocator.Instance.FileSystem, AbstractionsLocator.Instance.EnvironmentSystem, AbstractionsLocator.Instance.ProcessRunner)
        {            
        }

        internal ToolsHelper(IFileSystem fileSystem, IEnvironmentSystem environmentSystem, IProcessRunner processRunner)
        {
            FileSystem = fileSystem;
            EnvironmentSystem = environmentSystem;
            ProcessRunner = processRunner;
        }
        
        /// <summary>
        ///     Try to download tools from (if they don't exist):
        ///         - the path in the tool element in the configuration corresponding to tool name
        ///         - default tools path specified in tools defaultRemoteToolsPath=""
        ///     and copies it into tools defaultLocalToolsPath="" 
        /// </summary>
        public string DownloadTool(
            string toolName,
            string sourcePath = "",
            string targetPath = "",
            bool addToFirewall = false)
        {
            if (string.IsNullOrEmpty(toolName))
            {
                throw new ArgumentNullException(toolName);
            }

            // set default values
            sourcePath = string.IsNullOrEmpty(sourcePath)
                            ? TestEasyConfig.Instance.Tools.DefaultRemoteToolsPath
                            : sourcePath;

            targetPath = string.IsNullOrEmpty(targetPath)
                            ? TestEasyConfig.Instance.Tools.DefaultLocalToolsPath
                            : targetPath;


            // see if tool is registered in config and get config's path
            var toolElement = TestEasyConfig.Instance.Tools.Tools[toolName.ToLowerInvariant()];
            toolName = toolElement != null ? toolElement.Path : toolName;

            if (FileSystem.FileExists(toolName))
            {
                // if tool path is provided, then use it instead of default tools path 
                sourcePath = Path.GetDirectoryName(toolName) ?? "";
            }

            toolName = Path.GetFileName(toolName) ?? toolName;

            // prepare target path
            var currentDirectory = FileSystem.GetExecutingAssemblyDirectory();
            targetPath = string.IsNullOrEmpty(targetPath) ? currentDirectory : EnvironmentSystem.ExpandEnvironmentVariables(targetPath);

            if (!FileSystem.DirectoryExists(targetPath))
            {
                FileSystem.DirectoryCreate(targetPath);
            }

            var fullPathSource = Path.Combine(sourcePath, toolName);
            var fullPathTarget = Path.Combine(targetPath, toolName);

            // if tool was not copied before - copy it
            if (!FileSystem.FileExists(fullPathTarget) || WasFileModified(fullPathSource, fullPathTarget))
            {
                if (!FileSystem.FileExists(fullPathSource))
                {
                    throw new FileNotFoundException(
                        string.Format(
                            "Tool '{0}' was not found and can not be copied to current folder. Please make sure path '{1}' exists or use another Browser to work around.",
                            toolName, sourcePath));
                }

                FileSystem.FileCopy(fullPathSource, fullPathTarget, true);

                if (addToFirewall)
                {
                    var firewallHelper = new FirewallHelper(ProcessRunner);
                    firewallHelper.AddProgramToFirewall(fullPathTarget, "TestEasyTool");
                }
            }

            return fullPathTarget;
        }

        private  bool WasFileModified(string source, string dest)
        {
            if (!FileSystem.FileExists(source) || !FileSystem.FileExists(dest)) return true;

            return (FileSystem.FileGetLastWriteTime(source) > FileSystem.FileGetLastWriteTime(dest));
        }

        /// <summary>
        ///     Get unique folder name
        /// </summary>
        /// <returns></returns>
        public virtual string GetUniqueTempPath()
        {
            return GetUniqueTempPath("");
        }

        /// <summary>
        ///     Get unique folder name
        /// </summary>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public virtual string GetUniqueTempPath(string folderName)
        {
            folderName = String.IsNullOrEmpty(folderName) ? "Temp" : folderName;

            var root = FileSystem.GetTempPath();

            if (!FileSystem.DirectoryExists(root))
            {
                FileSystem.DirectoryCreate(root);
            }

            var counter = 0;
            var basePath = Path.Combine(root, folderName);
            var uniquePath = basePath;
            while (FileSystem.DirectoryExists(uniquePath))
            {
                uniquePath = basePath + "_" + (++counter);
            }

            FileSystem.DirectoryCreate(uniquePath);

            return uniquePath;
        }
    }
}
