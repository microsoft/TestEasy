using System;
using System.IO;
using Microsoft.Web.Administration;
using TestEasy.Core;
using TestEasy.Core.Abstractions;

namespace TestEasy.WebServer
{
    /// <summary>
    ///     Extensions for Microsoft.Web.Administration.Application
    /// </summary>
    public static class ApplicationExtensions
    {
        /// <summary>
        ///     Deploy folder to web application
        /// </summary>
        /// <param name="application"></param>
        /// <param name="sourceDir"></param>
        public static void Deploy(this Application application, string sourceDir)
        {
            Deploy(application, new DirectoryInfo(sourceDir));
        }

        /// <summary>
        ///     Deploy folder to web application
        /// </summary>
        /// <param name="application"></param>
        /// <param name="sourceDir"></param>
        public static void Deploy(this Application application, DirectoryInfo sourceDir)
        {
            Deploy(application, sourceDir.FullName, Dependencies.Instance.FileSystem);
        }

        /// <summary>
        ///     Deploy folder to web application under relative path
        /// </summary>
        /// <param name="application"></param>
        /// <param name="sourceDir"></param>
        /// <param name="relativePathUnderVDir"></param>
        public static void Deploy(this Application application, DirectoryInfo sourceDir, string relativePathUnderVDir)
        {
            Deploy(application, sourceDir.FullName, Dependencies.Instance.FileSystem, relativePathUnderVDir);
        }

        /// <summary>
        ///     Deploy file contents to web application
        /// </summary>
        /// <param name="application"></param>
        /// <param name="relativeFilePath"></param>
        /// <param name="fileContents"></param>
        public static void Deploy(this Application application, string relativeFilePath, string fileContents)
        {
            Deploy(application, relativeFilePath, fileContents, Dependencies.Instance.FileSystem);
        }

        /// <summary>
        ///     Deploy a list of files to web application
        /// </summary>
        /// <param name="application"></param>
        /// <param name="filePaths"></param>
        public static void Deploy(this Application application, string[] filePaths)
        {
            Deploy(application, filePaths, "");
        }

        /// <summary>
        ///     Deploy a list of files to web application under relative path
        /// </summary>
        /// <param name="application"></param>
        /// <param name="filePaths"></param>
        /// <param name="relativePathUnderVDir"></param>
        public static void Deploy(this Application application, string[] filePaths, string relativePathUnderVDir)
        {
            Deploy(application, filePaths, relativePathUnderVDir, Dependencies.Instance.FileSystem);
        }

        internal static void Deploy(Application application, string sourceDir, IFileSystem fileSystem, string relativePathUnderVDir = "")
        {
            if (!fileSystem.DirectoryExists(sourceDir))
            {
                throw new Exception(string.Format("Failed to deploy files to application, source directory does not exist: '{0}'.", sourceDir));
            }

            if (application.VirtualDirectories.Count <= 0)
            {
                throw new Exception(string.Format("Application '{0}' does not have a virtual directory.", application.Path));
            }

            var physicalPath = application.VirtualDirectories[0].PhysicalPath;
            if (!fileSystem.DirectoryExists(physicalPath))
            {
                fileSystem.DirectoryCreate(physicalPath);
            }

            var relativeDirectoryPath = Path.Combine(physicalPath, relativePathUnderVDir);
            if (!fileSystem.DirectoryExists(relativeDirectoryPath))
            {
                fileSystem.DirectoryCreate(relativeDirectoryPath);
            }

            fileSystem.DirectoryCopy(sourceDir, relativeDirectoryPath);
            if (string.IsNullOrEmpty(relativeDirectoryPath))
            {
                fileSystem.DirectorySetAttribute(relativeDirectoryPath, FileAttributes.Normal);
            }
        }

        internal static void Deploy(Application application, string relativeFilePath, string fileContents, IFileSystem fileSystem)
        {
            if (application.VirtualDirectories.Count <= 0)
            {
                throw new Exception(string.Format("Application '{0}' does not have a virtual directory.", application.Path));
            }

            var targetFilePath = Path.Combine(application.VirtualDirectories[0].PhysicalPath, relativeFilePath);

            fileSystem.FileWrite(targetFilePath, fileContents);
        }

        internal static void Deploy(Application application, string[] filePaths, string relativePathUnderVDir, IFileSystem fileSystem)
        {
            if (filePaths == null)
            {
                throw new ArgumentNullException("filePaths");
            }

            if (application.VirtualDirectories.Count <= 0)
            {
                throw new Exception(string.Format("Application '{0}' does not have a virtual directory.", application.Path));
            }

            var physicalPath = application.VirtualDirectories[0].PhysicalPath;
            if (!fileSystem.DirectoryExists(physicalPath))
            {
                fileSystem.DirectoryCreate(physicalPath);
            }

            var relativeDirectoryPath = Path.Combine(physicalPath, relativePathUnderVDir);
            if (!fileSystem.DirectoryExists(relativeDirectoryPath))
            {
                fileSystem.DirectoryCreate(relativeDirectoryPath);
            }

            foreach (var sourceFilePath in filePaths)
            {
                if (fileSystem.FileExists(sourceFilePath))
                {
                    var sourceFileName = Path.GetFileName(sourceFilePath) ?? sourceFilePath;
                    var destinationFileName = Path.Combine(relativeDirectoryPath, sourceFileName);
                    fileSystem.FileCopy(sourceFilePath, destinationFileName, true);
                }
            }
        }

        /// <summary>
        ///     Build web application
        /// </summary>
        /// <param name="application"></param>
        /// <param name="appName"></param>
        public static void Build(this Application application, string appName)
        {
            if (application.VirtualDirectories.Count <= 0)
            {
                throw new Exception(string.Format("Application '{0}' does not have VirtualDirectories associated with it, physical path can't be determined.", appName));
            }

            TestEasyHelpers.Builder.Build(application.VirtualDirectories[0].PhysicalPath, appName);
        }
    }
}
