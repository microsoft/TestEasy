using System;
using Xunit;

namespace TestEasy.WebServer.Test
{
    public class ApplicationExtensionsFacts
    {
        public class Deploy
        {
            [Fact]
            public void WhenSourceDoesNotExist_ShouldThrow()
            {
                // Arrange  
                var mock = new WebServerMockGenerator()
                            .MockApplicationDeploySourceDoesNotExist();

                // Act, Assert
                var exception = Assert.Throws<Exception>(() => ApplicationExtensions.Deploy(
                    mock.ServerManager.Sites[0].Applications[0], 
                    @"c:\Temp\MySite", 
                    mock.FileSystem.Object));
                Assert.Equal(@"Failed to deploy files to application, source directory does not exist: 'c:\Temp\MySite'.", exception.Message);
            }

            [Fact]
            public void WhenVirtualDirectoryDoesNotExist_ShouldThrow()
            {
                // Arrange
                var mock = new WebServerMockGenerator()
                            .MockApplication()
                            .MockApplicationVirtualDirectoryDoesNotExist();
                // Act, Assert
                var exception = Assert.Throws<Exception>(() => ApplicationExtensions.Deploy(
                    mock.ServerManager.Sites[0].Applications[mock.MockApplicationName], 
                    @"c:\Temp\MySite", 
                    mock.FileSystem.Object));
                Assert.Equal(string.Format(@"Application '{0}' does not have a virtual directory.", mock.MockApplicationName), exception.Message);
            }

            [Fact]
            public void WhenVirtualDirectoryPathNotExist_ShouldCreate()
            {
                // Arrange
                var mock = new WebServerMockGenerator()
                            .MockApplication()
                            .MockApplicationDeployVirtualDirectoryPathNotExistShouldCreate();

                // Act, Assert
                ApplicationExtensions.Deploy(
                    mock.ServerManager.Sites[0].Applications[mock.MockApplicationName],
                    @"c:\Temp\MySite",
                    mock.FileSystem.Object);
            }

            [Fact]
            public void WhenVirtualDirectoryPathExist_ShouldJustCopy()
            {
                // Arrange
                var mock = new WebServerMockGenerator()
                           .MockApplication()
                           .MockApplicationDeployVirtualDirectoryPathExistShouldJustCopy();

                // Act, Assert
                ApplicationExtensions.Deploy(
                    mock.ServerManager.Sites[0].Applications[mock.MockApplicationName],
                    @"c:\Temp\MySite",
                    mock.FileSystem.Object);
            }

            [Fact]
            public void WhenVirtualDirectoryPathExist_IfRelativePathProvided_ShouldCopyToRelative()
            {
                // Arrange
                var mock = new WebServerMockGenerator()
                           .MockApplication()
                           .MockApplicationDeployVirtualDirectoryPathExistIfRelativePathProvidedShouldCopyToRelative();

                // Act, Assert
                ApplicationExtensions.Deploy(
                    mock.ServerManager.Sites[0].Applications[mock.MockApplicationName],
                    @"c:\Temp\MySite",
                    mock.FileSystem.Object,
                    @"..\otherfolder");
            }

            [Fact]
            public void WhenDeployingListOfFiles_IfListIsEmpty_ShouldThrow()
            {
                // Arrange
                var mock = new WebServerMockGenerator()
                           .MockApplication()
                           .MockApplicationDeployListOfFilesIfListIsEmptyShouldThrow(); 

                // Act, Assert
                var exception = Assert.Throws<ArgumentNullException>(() => ApplicationExtensions.Deploy(
                    mock.ServerManager.Sites[0].Applications[mock.MockApplicationName], 
                    (string[])null, 
                    "", 
                    mock.FileSystem.Object));
                Assert.True(exception.Message.Contains("filePaths"));
            }

            [Fact]
            public void WhenDeployingListOfFiles_IfVirtualDirectoryDoesNotExist_ShouldThrow()
            {
                // Arrange
                var mock = new WebServerMockGenerator()
                           .MockApplication()
                           .MockApplicationPhysicalDirectoryExist()
                           .MockApplicationVirtualDirectoryDoesNotExist();

                // Act, Assert
                var exception = Assert.Throws<Exception>(() => ApplicationExtensions.Deploy(
                    mock.ServerManager.Sites[0].Applications[mock.MockApplicationName],
                    new[] 
                    {
                        @"c:\Temp\MySite\file1.html",
                        @"c:\Temp\MySite\file2.html",
                        @"c:\Temp\MySite\file3.html"
                    },
                    "",
                    mock.FileSystem.Object));
                Assert.Equal(string.Format(@"Application '{0}' does not have a virtual directory.", mock.MockApplicationName), exception.Message);
            }

            [Fact]
            public void WhenDeployingListOfFiles_ShouldCopyExistingFilesToRelativeSubFolder()
            {
                // Arrange
                var mock = new WebServerMockGenerator()
                           .MockApplication()
                           .MockApplicationDeployListOfFilesShouldCopyExistingFilesToRelativeSubFolder(); 

                // Act, Assert
                ApplicationExtensions.Deploy(
                    mock.ServerManager.Sites[0].Applications[mock.MockApplicationName], 
                    new [] 
                    {
                        @"c:\Temp\MySite\file1.html",
                        @"c:\Temp\MySite\file2.html",
                        @"c:\Temp\MySite\file3.html"
                    },
                    @"relative", 
                    mock.FileSystem.Object);
            }

            [Fact]
            public void WhenDeployingFileContent_ShouldCreateFile()
            {
                // Arrange
                var mock = new WebServerMockGenerator()
                           .MockApplication()
                           .MockApplicationDeployFileContentShouldCreateFile();

                // Act, Assert
                ApplicationExtensions.Deploy(
                    mock.ServerManager.Sites[0].Applications[mock.MockApplicationName],
                    mock.MockRelativePath,
                    mock.MockSampleFileContent,
                    mock.FileSystem.Object);
            }
        }
    }
}
