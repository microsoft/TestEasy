using System;
using System.Diagnostics;
using System.IO;
using Xunit;
using Moq;
using TestEasy.Core.Abstractions;
using TestEasy.Core.Configuration;
using TestEasy.Core.Helpers;

namespace TestEasy.Core.Test
{
    public class ToolsHelperFacts
    {
        public class DownloadTool
        {
            public DownloadTool()
            {
                TestEasyConfig.Instance = new TestEasyConfig(null);
            }

            [Fact]
            public void WhenDownloadTool_IfToolNameIsNullOrEmpty_ShouldThrow()
            {
                var helper = new ToolsHelper(null, null, null);

                // Arrange
                // Act       
                // Assert
                Assert.Throws<ArgumentNullException>(() => helper.DownloadTool(""));

                // Arrange
                // Act       
                // Assert
                Assert.Throws<ArgumentNullException>(() => helper.DownloadTool(null));
            }

            [Fact]
            public void WhenDownloadTool_IfSourcePathEmptyAndTaergetPathEmpty_ShouldTakePathsFromConfig()
            {
                // Arrange
                var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(m => m.FileExists(@"mytool.exe")).Returns(false);
                mockFileSystem.Setup(m => m.GetExecutingAssemblyDirectory()).Returns(@"x:\somelocalpath");
                mockFileSystem.Setup(m => m.DirectoryExists(@"x:\localpath")).Returns(true);
                mockFileSystem.Setup(m => m.FileExists(@"x:\localpath\mytool.exe")).Returns(true);
                mockFileSystem.Setup(m => m.FileExists(@"x:\sourcepath\mytool.exe")).Returns(true);
                mockFileSystem.Setup(m => m.FileGetLastWriteTime(@"x:\sourcepath\mytool.exe")).Returns(new DateTime(2000, 1, 1));
                mockFileSystem.Setup(m => m.FileGetLastWriteTime(@"x:\localpath\mytool.exe")).Returns(new DateTime(2000, 1, 1));

                var mockEnvironmentSystem = new Mock<IEnvironmentSystem>(MockBehavior.Strict);
                mockEnvironmentSystem.Setup(m => m.ExpandEnvironmentVariables(@"x:\localpath")).Returns(@"x:\localpath");

                TestEasyConfig.Instance.Tools.DefaultRemoteToolsPath = @"x:\sourcepath";
                TestEasyConfig.Instance.Tools.DefaultLocalToolsPath = @"x:\localpath";

                // Act       
                var helper = new ToolsHelper(mockFileSystem.Object, mockEnvironmentSystem.Object, null);
                var result = helper.DownloadTool("mytool.exe");

                // Assert
                Assert.Equal(@"x:\localpath\mytool.exe", result);
            }

            [Fact]
            public void WhenDownloadTool_IfSourcePathProvidedAndToolExists_ShouldTakeSourcePath()
            {
                // Arrange
                var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(m => m.FileExists(@"mytool.exe")).Returns(false);
                mockFileSystem.Setup(m => m.GetExecutingAssemblyDirectory()).Returns(@"x:\localpath");
                mockFileSystem.Setup(m => m.DirectoryExists(@"x:\localpath")).Returns(true);
                mockFileSystem.Setup(m => m.FileExists(@"x:\localpath\mytool.exe")).Returns(true);
                mockFileSystem.Setup(m => m.FileExists(@"x:\sourcepath\mytool.exe")).Returns(true);
                mockFileSystem.Setup(m => m.FileGetLastWriteTime(@"x:\sourcepath\mytool.exe")).Returns(new DateTime(2000, 1, 1));
                mockFileSystem.Setup(m => m.FileGetLastWriteTime(@"x:\localpath\mytool.exe")).Returns(new DateTime(2000, 1, 1));

                var mockEnvironmentSystem = new Mock<IEnvironmentSystem>(MockBehavior.Strict);
                mockEnvironmentSystem.Setup(m => m.ExpandEnvironmentVariables(@"x:\localpath")).Returns(@"x:\localpath");

                TestEasyConfig.Instance.Tools.DefaultLocalToolsPath = @"x:\localpath";

                // Act       
                var helper = new ToolsHelper(mockFileSystem.Object, mockEnvironmentSystem.Object, null);
                var result = helper.DownloadTool("mytool.exe", @"x:\sourcepath");

                // Assert
                Assert.Equal(@"x:\localpath\mytool.exe", result);
            }

            [Fact]
            public void WhenDownloadTool_IfSourcePathProvidedAndTargetPathProvided_ShouldUseThem()
            {
                // Arrange
                var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(m => m.FileExists(@"mytool.exe")).Returns(false);
                mockFileSystem.Setup(m => m.GetExecutingAssemblyDirectory()).Returns(@"x:\somelocalpath");
                mockFileSystem.Setup(m => m.DirectoryExists(@"x:\localpath")).Returns(true);
                mockFileSystem.Setup(m => m.FileExists(@"x:\localpath\mytool.exe")).Returns(true);
                mockFileSystem.Setup(m => m.FileExists(@"x:\sourcepath\mytool.exe")).Returns(true);
                mockFileSystem.Setup(m => m.FileGetLastWriteTime(@"x:\sourcepath\mytool.exe")).Returns(new DateTime(2000, 1, 1));
                mockFileSystem.Setup(m => m.FileGetLastWriteTime(@"x:\localpath\mytool.exe")).Returns(new DateTime(2000, 1, 1));

                var mockEnvironmentSystem = new Mock<IEnvironmentSystem>(MockBehavior.Strict);
                mockEnvironmentSystem.Setup(m => m.ExpandEnvironmentVariables(@"x:\localpath")).Returns(@"x:\localpath");

                TestEasyConfig.Instance.Tools.DefaultLocalToolsPath = @"x:\localpath";

                // Act       
                var helper = new ToolsHelper(mockFileSystem.Object, mockEnvironmentSystem.Object, null);
                var result = helper.DownloadTool("mytool.exe", @"x:\sourcepath", @"x:\localpath");

                // Assert
                Assert.Equal(@"x:\localpath\mytool.exe", result);
            }

            [Fact]
            public void WhenDownloadTool_IfToolIsInTheConfig_ShouldTakePathFromConfig()
            {
                // Arrange
                var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(m => m.FileExists(@"mytool.exe")).Returns(false);
                mockFileSystem.Setup(m => m.GetExecutingAssemblyDirectory()).Returns(@"x:\somelocalpath");
                mockFileSystem.Setup(m => m.DirectoryExists(@"x:\localpath")).Returns(false);
                mockFileSystem.Setup(m => m.DirectoryCreate(@"x:\localpath"));
                mockFileSystem.Setup(m => m.FileExists(@"x:\localpath\mytoolqq.exe")).Returns(true);
                mockFileSystem.Setup(m => m.FileExists(@"x:\sourcepathfromconfig\mytoolqq.exe")).Returns(true);
                mockFileSystem.Setup(m => m.FileGetLastWriteTime(@"x:\sourcepathfromconfig\mytoolqq.exe")).Returns(new DateTime(2000, 1, 1));
                mockFileSystem.Setup(m => m.FileGetLastWriteTime(@"x:\localpath\mytoolqq.exe")).Returns(new DateTime(2000, 1, 1));

                var mockEnvironmentSystem = new Mock<IEnvironmentSystem>(MockBehavior.Strict);
                mockEnvironmentSystem.Setup(m => m.ExpandEnvironmentVariables(@"x:\localpath")).Returns(@"x:\localpath");
                
                TestEasyConfig.Instance.Tools.Tools.Add(new ToolElement { Name = "mytool.exe", Path = @"x:\sourcepathfromconfig\mytoolqq.exe" });

                // Act       
                var helper = new ToolsHelper(mockFileSystem.Object, mockEnvironmentSystem.Object, null);
                var result = helper.DownloadTool("mytool.exe", @"x:\sourcepath", @"x:\localpath");

                // Assert
                Assert.Equal(@"x:\localpath\mytoolqq.exe", result);
            }

            [Fact]
            public void WhenDownloadTool_IfTargetNotExists_ShouldCreateIt()
            {
                // Arrange
                var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(m => m.FileExists(@"mytool.exe")).Returns(false);
                mockFileSystem.Setup(m => m.GetExecutingAssemblyDirectory()).Returns(@"x:\somelocalpath");
                mockFileSystem.Setup(m => m.DirectoryExists(@"x:\localpathexpanded")).Returns(true);
                mockFileSystem.Setup(m => m.FileExists(@"x:\localpathexpanded\mytool.exe")).Returns(false);
                mockFileSystem.Setup(m => m.FileExists(@"x:\sourcepath\mytool.exe")).Returns(true);
                mockFileSystem.Setup(m => m.FileCopy(@"x:\sourcepath\mytool.exe", @"x:\localpathexpanded\mytool.exe", true));

                var mockEnvironmentSystem = new Mock<IEnvironmentSystem>(MockBehavior.Strict);
                mockEnvironmentSystem.Setup(m => m.ExpandEnvironmentVariables(@"x:\localpath")).Returns(@"x:\localpathexpanded");

                // Act       
                var helper = new ToolsHelper(mockFileSystem.Object, mockEnvironmentSystem.Object, null);
                var result = helper.DownloadTool("mytool.exe", @"x:\sourcepath", @"x:\localpath");

                // Assert
                Assert.Equal(@"x:\localpathexpanded\mytool.exe", result);
            }

            [Fact]
            public void WhenDownloadTool_IfSourceNotExists_ShouldThrow()
            {
                // Arrange
                var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(m => m.FileExists(@"mytool.exe")).Returns(false);
                mockFileSystem.Setup(m => m.GetExecutingAssemblyDirectory()).Returns(@"x:\somelocalpath");
                mockFileSystem.Setup(m => m.DirectoryExists(@"x:\localpathexpanded")).Returns(true);
                mockFileSystem.Setup(m => m.FileExists(@"x:\localpathexpanded\mytool.exe")).Returns(false);
                mockFileSystem.Setup(m => m.FileExists(@"x:\sourcepath\mytool.exe")).Returns(false);

                var mockEnvironmentSystem = new Mock<IEnvironmentSystem>(MockBehavior.Strict);
                mockEnvironmentSystem.Setup(m => m.ExpandEnvironmentVariables(@"x:\localpath")).Returns(@"x:\localpathexpanded");

                var helper = new ToolsHelper(mockFileSystem.Object, mockEnvironmentSystem.Object, null);

                // Act       
                var exception = Assert.Throws<FileNotFoundException>(
                    () => helper.DownloadTool("mytool.exe", @"x:\sourcepath", @"x:\localpath"));
                Assert.Equal(@"Tool 'mytool.exe' was not found and can not be copied to current folder. Please make sure path 'x:\sourcepath' exists or use another Browser to work around.", 
                    exception.Message);
            }

            [Fact]
            public void WhenDownloadTool_IfFirewallTrue_ShouldAddToolToFirewall()
            {
                // Arrange
                var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(m => m.FileExists(@"mytool.exe")).Returns(false);
                mockFileSystem.Setup(m => m.GetExecutingAssemblyDirectory()).Returns(@"x:\somelocalpath");
                mockFileSystem.Setup(m => m.DirectoryExists(@"x:\localpathexpanded")).Returns(true);
                mockFileSystem.Setup(m => m.FileExists(@"x:\localpathexpanded\mytool.exe")).Returns(false);
                mockFileSystem.Setup(m => m.FileExists(@"x:\sourcepath\mytool.exe")).Returns(true);
                mockFileSystem.Setup(m => m.FileCopy(@"x:\sourcepath\mytool.exe", @"x:\localpathexpanded\mytool.exe", true));

                var mockEnvironmentSystem = new Mock<IEnvironmentSystem>(MockBehavior.Strict);
                mockEnvironmentSystem.Setup(m => m.ExpandEnvironmentVariables(@"x:\localpath")).Returns(@"x:\localpathexpanded");

                var mockProcessRunner = new Mock<IProcessRunner>(MockBehavior.Strict);
                mockProcessRunner.Setup(m => m.Start(It.Is<Process>(
                                        p => p.StartInfo.FileName == "netsh.exe" && p.StartInfo.Arguments == @"firewall add allowedprogram ""x:\localpathexpanded\mytool.exe"" TestEasyTool enable")))
                                 .Returns(true);
                mockProcessRunner.Setup(m => m.WaitForExit(It.Is<Process>(
                                        p => p.StartInfo.FileName == "netsh.exe" && p.StartInfo.Arguments == @"firewall add allowedprogram ""x:\localpathexpanded\mytool.exe"" TestEasyTool enable"), 60000))
                                 .Returns(true);

                // Act       
                var helper = new ToolsHelper(mockFileSystem.Object, mockEnvironmentSystem.Object, mockProcessRunner.Object);
                var result = helper.DownloadTool("mytool.exe", @"x:\sourcepath", @"x:\localpath", true);

                // Assert
                Assert.Equal(@"x:\localpathexpanded\mytool.exe", result);
            }
        }

        public class GetUniqueTempPath
        {
            [Fact]
            public void WhenGetUniqueTempPath_IfCalledFirstTime_ShouldCreateTempRootAndFirstSubFolder()
            {
                // Arrange
                var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(m => m.GetTempPath()).Returns(@"x:\temppath");
                mockFileSystem.Setup(m => m.DirectoryExists(@"x:\temppath")).Returns(false);
                mockFileSystem.Setup(m => m.DirectoryCreate(@"x:\temppath"));
                mockFileSystem.Setup(m => m.DirectoryExists(@"x:\temppath\MyWebSite")).Returns(false);
                mockFileSystem.Setup(m => m.DirectoryCreate(@"x:\temppath\MyWebSite"));

                // Act       
                var helper = new ToolsHelper(mockFileSystem.Object, null, null);
                var result = helper.GetUniqueTempPath("MyWebSite");

                // Assert
                Assert.Equal(@"x:\temppath\MyWebSite", result);
            }

            [Fact]
            public void WhenGetUniqueTempPath_IfFolderExists_ShouldIncreaseIndex()
            {
                // Arrange
                var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(m => m.GetTempPath()).Returns(@"x:\temppath");
                mockFileSystem.Setup(m => m.DirectoryExists(@"x:\temppath")).Returns(true);
                mockFileSystem.Setup(m => m.DirectoryExists(@"x:\temppath\MyWebSite")).Returns(true);
                mockFileSystem.Setup(m => m.DirectoryExists(@"x:\temppath\MyWebSite_1")).Returns(true);
                mockFileSystem.Setup(m => m.DirectoryExists(@"x:\temppath\MyWebSite_2")).Returns(false);
                mockFileSystem.Setup(m => m.DirectoryCreate(@"x:\temppath\MyWebSite_2"));

                // Act       
                var helper = new ToolsHelper(mockFileSystem.Object, null, null);
                var result = helper.GetUniqueTempPath("MyWebSite");

                // Assert
                Assert.Equal(@"x:\temppath\MyWebSite_2", result);
            }
        }
    }
}
