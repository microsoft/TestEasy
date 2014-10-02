using System;
using System.IO;
using Xunit;
using TestEasy.Core.Helpers;

namespace TestEasy.Core.Test
{
    public class BuildHelperFacts
    {
        public class Build
        {
            [Fact]
            public void WhenBuild_IfNoPhysicalDirectoryExist_ShouldThrow()
            {
                // Arrange
                var mock = new CoreMockGenerator()
                           .MockApplication()
                           .MockApplicationPhysicalDirectoryDoesNotExist();

                // Act, Assert
                var helper = new BuildHelper(mock.FileSystem.Object, mock.EnvironmentSystem.Object, mock.ProcessRunner.Object);
                var exception = Assert.Throws<Exception>(() => helper.Build(
                    mock.MockApplicationPhysicalPath,
                    mock.MockApplicationName));

                Assert.Equal(string.Format("Application directory '{0}' does not exist.", mock.MockApplicationPhysicalPath), exception.Message);
            }

            [Fact]
            public void WhenBuild_IfNoProjectFileExist_ShouldThrow()
            {
                // Arrange
                var mock = new CoreMockGenerator()
                           .MockApplication()
                           .MockApplicationPhysicalDirectory()
                           .MockApplicationProjectFileDoesNotExist();

                // Act, Assert
                var helper = new BuildHelper(mock.FileSystem.Object, mock.EnvironmentSystem.Object, mock.ProcessRunner.Object);
                var exception = Assert.Throws<Exception>(() => helper.Build(
                    mock.MockApplicationPhysicalPath,
                    mock.MockApplicationName));

                Assert.Equal(string.Format("Application project file '{0}' does not exist.", 
                    Path.Combine(mock.MockApplicationPhysicalPath, mock.MockApplicationName + ".csproj")), exception.Message);
            }

            [Fact]
            public void WhenBuild_IfProcessStartReturnsFalse_ShouldThrow()
            {
                // Arrange
                var mock = new CoreMockGenerator();
                mock.MockApplication()
                    .MockApplicationPhysicalDirectory()
                    .MockApplicationProjectFile()
                    .MockGetMsBuildPathProgramFilesMsbuild()
                    .MockBuildProcessStartFalse(mock.MockProgramFilesMsbuilPath);

                // Act, Assert
                var helper = new BuildHelper(mock.FileSystem.Object, mock.EnvironmentSystem.Object, mock.ProcessRunner.Object);
                var exception = Assert.Throws<Exception>(() => helper.Build(
                    mock.MockApplicationPhysicalPath,
                    mock.MockApplicationName));

                Assert.Equal(string.Format(@"Failed to start msbuild to build a project '{0}' at '{1}'",
                            mock.MockApplicationName + ".csproj", mock.MockApplicationPhysicalPath), exception.Message);
            }

            [Fact]
            public void WhenBuild_IfProcessStartNormallyFromProgramFiles_ShouldWorkOk()
            {
                // Arrange
                var mock = new CoreMockGenerator();
                mock.MockApplication()
                    .MockApplicationPhysicalDirectory()
                    .MockApplicationProjectFile()
                    .MockGetMsBuildPathProgramFilesMsbuild()
                    .MockBuildProcessStartNormally(mock.MockProgramFilesMsbuilPath);

                // Act, Assert
                var helper = new BuildHelper(mock.FileSystem.Object, mock.EnvironmentSystem.Object, mock.ProcessRunner.Object);
                helper.Build(
                    mock.MockApplicationPhysicalPath,
                    mock.MockApplicationName);
            }

            [Fact]
            public void WhenBuild_IfProcessStartNormallyFromDotNet_ShouldWorkOk()
            {
                // Arrange
                var mock = new CoreMockGenerator();
                mock.MockApplication()
                    .MockApplicationPhysicalDirectory()
                    .MockApplicationProjectFile()
                    .MockGetMsBuildDotNetMsbuild()
                    .MockBuildProcessStartNormally(mock.MockDotNetMsbuilPath);

                // Act, Assert
                var helper = new BuildHelper(mock.FileSystem.Object, mock.EnvironmentSystem.Object, mock.ProcessRunner.Object);
                helper.Build(
                    mock.MockApplicationPhysicalPath,
                    mock.MockApplicationName);
            }

            [Fact]
            public void WhenBuild_IfMsbuildNotFound_ShouldThrow()
            {
                // Arrange
                var mock = new CoreMockGenerator();
                mock.MockApplication()
                    .MockApplication()
                    .MockApplicationPhysicalDirectory()
                    .MockApplicationProjectFile()
                    .MockGetMsBuildPathNotFound();

                // Act, Assert
                var helper = new BuildHelper(mock.FileSystem.Object, mock.EnvironmentSystem.Object, mock.ProcessRunner.Object);
                var exception = Assert.Throws<Exception>(() => helper.Build(
                    mock.MockApplicationPhysicalPath,
                    mock.MockApplicationName));

                Assert.Equal("MsBuild.exe was not found on the machine.", exception.Message);
            }

            [Fact]
            public void WhenBuild_IfBuildProcessExitCodeNotZero_ShouldThrow()
            {
                // Arrange
                var mock = new CoreMockGenerator();
                mock.MockApplication()
                    .MockApplicationPhysicalDirectory()
                    .MockApplicationProjectFile()
                    .MockGetMsBuildDotNetMsbuild()
                    .MockBuildProcessExitCodeNotZero(mock.MockDotNetMsbuilPath);

                // Act, Assert
                var helper = new BuildHelper(mock.FileSystem.Object, mock.EnvironmentSystem.Object, mock.ProcessRunner.Object);
                var exception = Assert.Throws<Exception>(() => helper.Build(
                    mock.MockApplicationPhysicalPath,
                    mock.MockApplicationName));

                Assert.Equal(string.Format("There were some errors while trying to build a project '{0}' at '{1}': {2}",
                    mock.MockApplicationName + ".csproj", mock.MockApplicationPhysicalPath, mock.MockBuildOutput), exception.Message);
            }
        }
    }
}
