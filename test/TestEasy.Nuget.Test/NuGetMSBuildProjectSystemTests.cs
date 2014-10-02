using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestEasy.Nuget.Test.Mocks;
using TestEasy.NuGet;
using Xunit;
using Xunit.Extensions;

namespace TestEasy.Nuget.Test
{
    public class NuGetMSBuildProjectSystemTests
    {
        public class Ctor
        {
            [Fact]
            public void RootIsContainingFolder()
            {
                // act
                var projectSystem = new NuGetMsBuildProjectSystem(@"C:\DummyPath\Dummy.csproj", MockGenerator.CreateMSBuildProjectFactory());

                // assert
                Assert.Equal(projectSystem.Root, @"C:\DummyPath");

            }
        }
        public class AddFrameworkReference
        {
            [Fact]
            public void IfNameNullOrEmpty_Throws()
            {
                var projectSystem = new NuGetMsBuildProjectSystem(@"C:\DummyPath\Dummy.csproj", MockGenerator.CreateMSBuildProjectFactory());

                Assert.Throws<ArgumentNullException>(() => projectSystem.AddFrameworkReference(null));
                Assert.Throws<ArgumentNullException>(() => projectSystem.AddFrameworkReference(string.Empty));
            }

            [Fact]
            public void AddingFrameworkReference_CallSaveProject()
            {
                // arrange 
                var mockProject = MockGenerator.CreateMockMSBuildProject_SaveMethodVerifiable();

                // act
                var projectSystem = new NuGetMsBuildProjectSystem(@"C:\DummyPath\Dummy.csproj", MockGenerator.CreateMSBuildProjectFactory(mockProject.Object));
                projectSystem.AddFrameworkReference("System.Web");

                // assert
                mockProject.Verify();
            }
        }

        public class AddReference
        {
            [Fact]
            public void IfNameNullOrEmpty_Throws()
            {
                var projectSystem = new NuGetMsBuildProjectSystem(@"C:\DummyPath\Dummy.csproj", MockGenerator.CreateMSBuildProjectFactory());

                Assert.Throws<ArgumentNullException>(() => projectSystem.AddReference(null, Stream.Null));
                Assert.Throws<ArgumentNullException>(() => projectSystem.AddReference(string.Empty, Stream.Null));
            }

            [Theory]
            [InlineData(@"C:\DummyPath\packages\Foo.dll", @"packages\Foo.dll")]
            [InlineData(@"DummyPath\packages\Foo.dll", @"packages\Foo.dll")] // this is the default case
            [InlineData(@"C:\DifferentPath\packages\Foo.dll", @"..\DifferentPath\packages\Foo.dll")]
            [InlineData(@"D:\DifferentDrive\Foo.dll", @"D:\DifferentDrive\Foo.dll")]
            [InlineData(@"\\UNC\Share\Foo.dll", @"\\UNC\Share\Foo.dll")]
            public void UseRelativeHintPath(string fullPath, string hintPath)
            {
                // arrange
                var mockProject = MockGenerator.CreateMockMSBuildProject();
                string actualHint = "";
                mockProject.Setup(p => p.AddReference(It.IsAny<string>(), It.IsAny<string>()))
                    .Callback<string, string>((_, hint) => actualHint = hint)
                    .Verifiable();

                // act
                var projectSystem = new NuGetMsBuildProjectSystem(@"C:\DummyPath\Dummy.csproj", MockGenerator.CreateMSBuildProjectFactory(mockProject.Object));
                projectSystem.AddReference(fullPath, Stream.Null);

                // assert
                Assert.Equal(hintPath, actualHint);
                mockProject.Verify();
            }

            [Fact]
            public void AddingReference_CallSaveProject()
            {
                // arrange 
                var mockProject = MockGenerator.CreateMockMSBuildProject_SaveMethodVerifiable();

                // act
                var projectSystem = new NuGetMsBuildProjectSystem(@"C:\DummyPath\Dummy.csproj", MockGenerator.CreateMSBuildProjectFactory(mockProject.Object));
                projectSystem.AddReference(@"C:\DummyPath\packages\Dummy.dll", Stream.Null);

                // assert
                mockProject.Verify();
            }

        }

        public class ReferenceExists
        {
            [Fact]
            public void IfNameNullOrEmpty_Throws()
            {
                var projectSystem = new NuGetMsBuildProjectSystem(@"C:\DummyPath\Dummy.csproj", MockGenerator.CreateMSBuildProjectFactory());

                Assert.Throws<ArgumentNullException>(() => projectSystem.ReferenceExists(null));
                Assert.Throws<ArgumentNullException>(() => projectSystem.ReferenceExists(string.Empty));
            }

            [Theory]
            [InlineData("Dummy.dll", "Dummy")]
            [InlineData("Dummy.exe", "Dummy")]
            [InlineData("System.Dummy", "System.Dummy")]
            public void StripExtensionFromReferenceName(string assemblyName, string expectedReferenceName)
            {
                // arrange
                var mockProject = MockGenerator.CreateMockMSBuildProject();
                mockProject.Setup(p => p.ReferenceExists(expectedReferenceName)).Verifiable();

                // act
                var projectSystem = new NuGetMsBuildProjectSystem(@"C:\DummyPath\Dummy.csproj", MockGenerator.CreateMSBuildProjectFactory(mockProject.Object));

                // assert
                Assert.False(projectSystem.ReferenceExists(assemblyName));
                mockProject.Verify();

            }

            [Fact]
            public void OnlySearchReferenceItemType()
            {
                // arrange
                var mockProject = MockGenerator.CreateMockMSBuildProject();
                mockProject.Setup(p => p.ReferenceExists("DummyReference")).Returns(false).Verifiable();
                mockProject.Setup(p => p.ItemExists("Content", "DummyReference")).Returns(true);

                // act
                var projectSystem = new NuGetMsBuildProjectSystem(@"C:\DummyPath\Dummy.csproj", MockGenerator.CreateMSBuildProjectFactory(mockProject.Object));

                // assert
                Assert.False(projectSystem.ReferenceExists("DummyReference.dll"));
                mockProject.Verify();
            }
        }

        public class RemoveReference
        {
            [Fact]
            public void RemovingReference_CallSaveProject()
            {
                // arrange 
                var mockProject = MockGenerator.CreateMockMSBuildProject_SaveMethodVerifiable();
                mockProject.Setup(p => p.ReferenceExists(It.Is<string>(s => s == "Dummy" || s == "Dummy.dll"))).Returns(true);
                mockProject.Setup(p => p.RemoveItem("Reference", "Dummy")).Verifiable();

                // act
                var projectSystem = new NuGetMsBuildProjectSystem(@"C:\DummyPath\Dummy.csproj", MockGenerator.CreateMSBuildProjectFactory(mockProject.Object));
                projectSystem.RemoveReference(@"Dummy.dll");

                // assert
                mockProject.Verify();
            }

            [Fact]
            public void IfReferenceDoesntExist_NoOp()
            {
                // arrange 
                var mockProject = new Mock<IMSBuildProject>(MockBehavior.Strict);
                mockProject.Setup(p => p.ReferenceExists(It.Is<string>(s => s == "Dummy" || s == "Dummy.dll"))).Returns(false).Verifiable();

                // act
                var projectSystem = new NuGetMsBuildProjectSystem(@"C:\DummyPath\Dummy.csproj", MockGenerator.CreateMSBuildProjectFactory(mockProject.Object));
                projectSystem.RemoveReference(@"Dummy.dll");

                // assert
                mockProject.Verify();
                // MockBehavior.Strict verifies that no others are called.
            }
        }
        
        public class ResolvePath
        {
            [Theory]
            [InlineData(@"Foo.cs", @"Foo.cs")]
            [InlineData(@"App_Code\Foo.cs", @"App_Code\Foo.cs")]
            [InlineData(@"Foo.aspx.cs", @"Foo.aspx.cs")]
            [InlineData(@"Foo.foo.cs", @"Foo.foo.cs")]
            [InlineData(@"Foo.aspx.garbage", @"Foo.aspx.garbage")]
            [InlineData(@"App_Start\Foo.cs", @"App_Start\Foo.cs")]
            [InlineData(@"Bar\Foo.cs", @"Bar\Foo.cs")]
            public void ResolvePaths_DoesNotModifyAnyPath(string input, string expected)
            {
                var projSystem = new NuGetMsBuildProjectSystem(@"Dummy\Dummy.csproj", MockGenerator.CreateMSBuildProjectFactory());
                Assert.Equal(expected, projSystem.ResolvePath(input));
            }
        }

        public class GetAssemblyReferencePaths
        {
            [Theory]
            [InlineData(@"packages\Foo.dll", new string[] {@"C:\Dummy\packages\Foo.dll"})]
            [InlineData(@"..\packages\Bar.dll", new string[] { @"C:\packages\Bar.dll"})]
            [InlineData(@"C:\AbsolutePath\Dummy.dll", new string[] { @"C:\AbsolutePath\Dummy.dll" })]
            public void ReturnsFullPaths(string inputPath, IEnumerable<string> expected)
            {
                // arrange
                var mockFileSystem = MockGenerator.CreateMockFileSystem();
                mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);

                var mockMSBuildProject = MockGenerator.CreateMockMSBuildProject();
                mockMSBuildProject.Setup(p => p.GetItemsWithMetadataProperty("Reference", "HintPath")).Returns(new Dictionary<string, string>() {
                    {Path.GetFileNameWithoutExtension(inputPath), inputPath}
                });

                Dependencies.FileSystem = mockFileSystem.Object;
                Dependencies.MSBuildProjectFactory = MockGenerator.CreateMSBuildProjectFactory(mockMSBuildProject.Object);

                // act
                var projectSystem = new NuGetMsBuildProjectSystem(@"C:\Dummy\Dummy.csproj");
                var results = projectSystem.GetAssemblyReferencePaths();

                // assert
                Assert.Equal(expected, results);
            }

            [Fact]
            public void OnlyIncludeFilesWhichExist()
            {
                // arrange
                var mockFileSystem = MockGenerator.CreateMockFileSystem();
                // mock Bar.dll to not exist
                mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns<string>((s) => !s.Contains("Bar.dll"));

                var mockMSBuildProject = MockGenerator.CreateMockMSBuildProject();
                mockMSBuildProject.Setup(p => p.GetItemsWithMetadataProperty("Reference", "HintPath")).Returns(new Dictionary<string, string>() {
                    {"Foo", @"packages\Foo.dll"},
                    {"Bar", @"packages\Bar.dll"},
                    {"Baz", @"packages\Baz.dll"}
                });

                Dependencies.FileSystem = mockFileSystem.Object;
                Dependencies.MSBuildProjectFactory = MockGenerator.CreateMSBuildProjectFactory(mockMSBuildProject.Object);

                // act
                var projectSystem = new NuGetMsBuildProjectSystem(@"C:\Dummy\Dummy.csproj");
                var results = projectSystem.GetAssemblyReferencePaths();

                // assert
                string[] expected = { @"C:\Dummy\packages\Foo.dll", @"C:\Dummy\packages\Baz.dll" };
                Assert.Equal(expected, results);
            }
        }
    }
}
