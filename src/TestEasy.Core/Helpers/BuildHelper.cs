using System;
using System.Diagnostics;
using System.IO;
using TestEasy.Core.Abstractions;

namespace TestEasy.Core.Helpers
{
    /// <summary>
    ///     Helper API for building web projects
    /// </summary>
    public class BuildHelper
    {
        private readonly IFileSystem _fileSystem;
        private readonly IEnvironmentSystem _environmentSystem;
        private readonly IProcessRunner _processRunner;

        /// <summary>
        ///     ctor
        /// </summary>
        public BuildHelper()
            :this(AbstractionsLocator.Instance.FileSystem, AbstractionsLocator.Instance.EnvironmentSystem, AbstractionsLocator.Instance.ProcessRunner)
        {            
        }

        internal BuildHelper(IFileSystem fileSystem, IEnvironmentSystem environmentSystem, IProcessRunner processRunner)
        {
            _fileSystem = fileSystem;
            _environmentSystem = environmentSystem;
            _processRunner = processRunner;
        }

        /// <summary>
        ///     Build web application project
        /// </summary>
        /// <param name="appPhysicalPath"></param>
        /// <param name="projectName"></param>
        public virtual void Build(string appPhysicalPath, string projectName)
        {
            if (!_fileSystem.DirectoryExists(appPhysicalPath))
            {
                throw new Exception(string.Format("Application directory '{0}' does not exist.", appPhysicalPath));
            }

            if (string.IsNullOrEmpty(Path.GetExtension(projectName)))
            {
                projectName = projectName + ".csproj";
            }

            var projectFilePath = Path.Combine(appPhysicalPath, projectName);
            if (!_fileSystem.FileExists(projectFilePath))
            {
                throw new Exception(string.Format("Application project file '{0}' does not exist.", projectFilePath));
            }

            var psi = new ProcessStartInfo
            {
                FileName = GetMsBuildPath(_fileSystem, _environmentSystem),
                Arguments = string.Format("\"{0}\"", projectFilePath),
                WorkingDirectory = appPhysicalPath,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var msbuild = new Process { StartInfo = psi };
            if (!_processRunner.Start(msbuild))
            {
                throw new Exception(string.Format("Failed to start msbuild to build a project '{0}' at '{1}'", projectName, appPhysicalPath));
            }

            var output = _processRunner.GetProcessOutput(msbuild);
            if (!_processRunner.WaitForExit(msbuild, 10000))
            {
                _processRunner.Stop(msbuild);
            }

            var exitCode = _processRunner.GetProcessExitCode(msbuild);
            if (exitCode != 0)
            {
                throw new Exception(string.Format("There were some errors while trying to build a project '{0}' at '{1}': {2}",
                    projectName, appPhysicalPath, output));
            }
        }

        private string GetMsBuildPath(IFileSystem fileSystem, IEnvironmentSystem environmentSystem)
        {
            var msBuildPaths = new[]
                {
                    environmentSystem.ExpandEnvironmentVariables(@"%ProgramFiles%\MSBuild\12.0\bin\msbuild.exe"),
                    environmentSystem.ExpandEnvironmentVariables(@"%SystemDrive%\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe")
                };

            foreach (var path in msBuildPaths)
            {
                if (fileSystem.FileExists(path)) return path;
            }

            throw new Exception("MsBuild.exe was not found on the machine.");
        }       
    }
}
