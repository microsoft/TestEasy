using System;
using System.Diagnostics;
using System.IO;
using Moq;
using TestEasy.TestHelpers;

namespace TestEasy.Core.Test
{
    class CoreMockGenerator : MockGenerator
    {
        public string MockApplicationName { get; set; }
        public string MockApplicationPhysicalPath { get; set; }
        public CoreMockGenerator MockApplication()
        {
            MockApplicationName = Randomize("testapp");
            MockApplicationPhysicalPath = @"c:\temp\MyApp";

            return this;
        }

        public CoreMockGenerator MockApplicationPhysicalDirectoryDoesNotExist()
        {
            FileSystem.Setup(f => f.DirectoryExists(MockApplicationPhysicalPath)).Returns(false);

            return this;
        }

        public CoreMockGenerator MockApplicationPhysicalDirectory()
        {
            FileSystem.Setup(f => f.DirectoryExists(MockApplicationPhysicalPath)).Returns(true);

            return this;
        }

        public CoreMockGenerator MockApplicationProjectFileDoesNotExist()
        {
            FileSystem.Setup(f => f.FileExists(Path.Combine(MockApplicationPhysicalPath, MockApplicationName + ".csproj"))).Returns(false);

            return this;
        }

        public CoreMockGenerator MockApplicationProjectFile()
        {
            FileSystem.Setup(f => f.FileExists(Path.Combine(MockApplicationPhysicalPath, MockApplicationName + ".csproj"))).Returns(true);

            return this;
        }

        public string MockProgramFilesMsbuilPath = @"c:\Program Files\Msbuild\msbuild.exe";
        public string MockDotNetMsbuilPath = @"c:\Windows\Framework\Msbuild\msbuild.exe";
        public CoreMockGenerator MockGetMsBuildPathProgramFilesMsbuild()
        {
            EnvironmentSystem.Setup(f => f.ExpandEnvironmentVariables(@"%ProgramFiles%\MSBuild\12.0\bin\msbuild.exe")).Returns(MockProgramFilesMsbuilPath);
            EnvironmentSystem.Setup(f => f.ExpandEnvironmentVariables(@"%SystemDrive%\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe")).Returns(MockDotNetMsbuilPath);
            FileSystem.Setup(f => f.FileExists(MockProgramFilesMsbuilPath)).Returns(true);

            return this;
        }

        public CoreMockGenerator MockGetMsBuildDotNetMsbuild()
        {
            EnvironmentSystem.Setup(f => f.ExpandEnvironmentVariables(@"%ProgramFiles%\MSBuild\12.0\bin\msbuild.exe")).Returns(MockProgramFilesMsbuilPath);
            EnvironmentSystem.Setup(f => f.ExpandEnvironmentVariables(@"%SystemDrive%\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe")).Returns(MockDotNetMsbuilPath);
            FileSystem.Setup(f => f.FileExists(MockProgramFilesMsbuilPath)).Returns(false);
            FileSystem.Setup(f => f.FileExists(MockDotNetMsbuilPath)).Returns(true);

            return this;
        }

        public CoreMockGenerator MockGetMsBuildPathNotFound()
        {
            EnvironmentSystem.Setup(f => f.ExpandEnvironmentVariables(@"%ProgramFiles%\MSBuild\12.0\bin\msbuild.exe")).Returns(MockProgramFilesMsbuilPath);
            EnvironmentSystem.Setup(f => f.ExpandEnvironmentVariables(@"%SystemDrive%\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe")).Returns(MockDotNetMsbuilPath);
            FileSystem.Setup(f => f.FileExists(MockProgramFilesMsbuilPath)).Returns(false);
            FileSystem.Setup(f => f.FileExists(MockDotNetMsbuilPath)).Returns(false);

            return this;
        }

        public CoreMockGenerator MockBuildProcessStartFalse(string processName)
        {
            ProcessRunner.Setup(f => f.Start(It.Is<Process>(p => p.StartInfo.FileName.Equals(processName, StringComparison.InvariantCultureIgnoreCase)))).Returns(false);

            return this;
        }

        public string MockBuildOutput = "some output";
        public CoreMockGenerator MockBuildProcessStartNormally(string processName)
        {
            ProcessRunner.Setup(f => f.Start(It.Is<Process>(p => p.StartInfo.FileName.Equals(processName, StringComparison.InvariantCultureIgnoreCase)))).Returns(true);
            ProcessRunner.Setup(f => f.GetProcessOutput(It.Is<Process>(p => p.StartInfo.FileName.Equals(processName, StringComparison.InvariantCultureIgnoreCase)))).Returns(MockBuildOutput);
            ProcessRunner.Setup(f => f.GetProcessExitCode(It.Is<Process>(p => p.StartInfo.FileName.Equals(processName, StringComparison.InvariantCultureIgnoreCase)))).Returns(0);
            ProcessRunner.Setup(f => f.WaitForExit(It.Is<Process>(p => p.StartInfo.FileName.Equals(processName, StringComparison.InvariantCultureIgnoreCase)), 10000)).Returns(true);

            return this;
        }
        public CoreMockGenerator MockBuildProcessExitCodeNotZero(string processName)
        {
            ProcessRunner.Setup(f => f.Start(It.Is<Process>(p => p.StartInfo.FileName.Equals(processName, StringComparison.InvariantCultureIgnoreCase)))).Returns(true);
            ProcessRunner.Setup(f => f.GetProcessOutput(It.Is<Process>(p => p.StartInfo.FileName.Equals(processName, StringComparison.InvariantCultureIgnoreCase)))).Returns(MockBuildOutput);
            ProcessRunner.Setup(f => f.GetProcessExitCode(It.Is<Process>(p => p.StartInfo.FileName.Equals(processName, StringComparison.InvariantCultureIgnoreCase)))).Returns(1);
            ProcessRunner.Setup(f => f.WaitForExit(It.Is<Process>(p => p.StartInfo.FileName.Equals(processName, StringComparison.InvariantCultureIgnoreCase)), 10000)).Returns(true);

            return this;
        }
    }
}
