using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestEasy.Core.Abstractions;
using TestEasy.Nuget.Test.Mocks;
using TestEasy.NuGet;
using Xunit;
using Xunit.Extensions;

namespace TestEasy.Nuget.Test
{
    public class NugetProjectSystemFactoryTests
    {
        public class CreateProject
        {

            [Theory]
            [InlineData(new object[] { new string[] { @"C:\DummyPath\Dummy.csproj" } })]
            [InlineData(new object[] { new string[] { @"C:\DummyPath\Dummy.vbproj" } })]
            public void FindProjectFile_CreateMSBuildProjectSystem(string[] files)
            {
                var mockFileSystem = new Mock<IFileSystem>();
                mockFileSystem.Setup(fs => fs.DirectoryGetFiles(@"C:\DummyPath")).Returns(files);

                // act
                var factory = new NuGetProjectFactory(mockFileSystem.Object, MockGenerator.CreateMSBuildProjectFactory());
                var project = factory.CreateProject(@"C:\DummyPath");

                // assert
                Assert.Equal(typeof(NuGetMsBuildProjectSystem), project.GetType());
            }

            [Fact]
            public void FindSolutionFileNoProjectFile_CreateWebProjectSystem()
            {
                var mockFileSystem = new Mock<IFileSystem>();
                mockFileSystem.Setup(fs => fs.DirectoryGetFiles(@"C:\DummyPath")).Returns(new string[] { "Dummy.sln" });

                // act
                var factory = new NuGetProjectFactory(mockFileSystem.Object);
                var project = factory.CreateProject(@"C:\DummyPath");

                // assert
                Assert.Equal(typeof(NuGetWebProjectSystem), project.GetType());
            }

            [Fact]
            public void IfNoProjectFiles_CreateWebProjectSystem()
            {
                // arrange
                var mockFileSystem = new Mock<IFileSystem>();

                // act
                var factory = new NuGetProjectFactory(mockFileSystem.Object);
                var project = factory.CreateProject(@"C:\DummyPath");

                // assert
                Assert.Equal(typeof(NuGetWebProjectSystem), project.GetType());
            }

            [Fact]
            public void IfUnableToLoadMSBuildProject_CreateWebProjectSystem()
            {
                // arrange
                var mockFileSystem = new Mock<IFileSystem>();
                mockFileSystem.Setup(fs => fs.DirectoryGetFiles(@"C:\DummyPath")).Returns(new string[] {@"C:\DummyPath\Dummy.csproj"});

                var mockMSBuildProjectFactory = new Mock<IMSBuildProjectFactory>();
                mockMSBuildProjectFactory.Setup(f => f.CreateProject(It.IsAny<string>()))
                    .Throws<Microsoft.Build.Exceptions.InvalidProjectFileException>();

                // act
                var factory = new NuGetProjectFactory(mockFileSystem.Object, mockMSBuildProjectFactory.Object);
                var project = factory.CreateProject(@"C:\DummyPath");

                // assert
                Assert.Equal(typeof(NuGetWebProjectSystem), project.GetType());
            }

        }

    }
}
