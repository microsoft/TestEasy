using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TestEasy.Core.Abstractions;
using TestEasy.NuGet;
using Xunit;
using Xunit.Extensions;

namespace TestEasy.Nuget.Test
{
    public class NuGetWebProjectSystemTests
    {
        public class AddReference
        {
            [Fact]
            public void EmptyReferencePath()
            {
                var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

                NuGetWebProjectSystem projSystem = new NuGetWebProjectSystem(@"C:\TestRoot", mockFileSystem.Object, Dependencies.AssemblyResolver);

                Assert.Throws<ArgumentNullException>(() => projSystem.AddReference(null, Stream.Null));
                Assert.Throws<ArgumentNullException>(() => projSystem.AddReference(@"", Stream.Null));
            }
        }

        public class GetReferenceFilesToAdd
        {
            [Fact]
            public void SingleAssembly()
            {
                // arrange
                var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
                string mockDirectory = @"C:\TestRoot\packages\FooPackage.1.0\lib";
                mockFileSystem.Setup(fs => fs.DirectoryGetSubDirs(mockDirectory)).Returns(new string[] { });
                mockFileSystem.Setup(fs => fs.FileExists(Path.Combine(mockDirectory, "FooPackage.dll"))).Returns(true);

                NuGetWebProjectSystem projSystem = new NuGetWebProjectSystem(@"C:\TestRoot", mockFileSystem.Object, Dependencies.AssemblyResolver);

                // act
                var files = projSystem.GetReferenceFilesToAdd(@"TestRoot\packages\FooPackage.1.0\lib\FooPackage.dll");

                // assert
                Dictionary<string, string> expected = new Dictionary<string,string>{ 
                    {@"C:\TestRoot\packages\FooPackage.1.0\lib\FooPackage.dll", @"C:\TestRoot\bin\FooPackage.dll" }
                };
                Assert.Equal<Dictionary<string, string>>(expected, files);
            }

            [Fact]
            public void IncludeSatelliteAssembly()
            {
                string mockDirectory = @"C:\TestRoot\packages\FooPackage.1.0\lib";
                string mockLocDirectory = Path.Combine(mockDirectory, "ja-JP");
                var mockFileSystem = new Mock<IFileSystem>();
                mockFileSystem.Setup(fs => fs.DirectoryGetSubDirs(mockDirectory)).Returns(new string[] { mockLocDirectory });
                mockFileSystem.Setup(fs => fs.DirectoryGetFiles(mockLocDirectory)).Returns(new string[] { Path.Combine(mockLocDirectory, "FooPackage.resources.dll") });
                mockFileSystem.Setup(fs => fs.FileExists(Path.Combine(mockDirectory, "FooPackage.dll"))).Returns(true);

                NuGetWebProjectSystem projSystem = new NuGetWebProjectSystem(@"C:\TestRoot", mockFileSystem.Object, Dependencies.AssemblyResolver);

                // act
                var files = projSystem.GetReferenceFilesToAdd(@"TestRoot\packages\FooPackage.1.0\lib\FooPackage.dll");

                // assert
                Dictionary<string, string> expected = new Dictionary<string, string>{ 
                    {@"C:\TestRoot\packages\FooPackage.1.0\lib\FooPackage.dll", @"C:\TestRoot\bin\FooPackage.dll" },
                    {@"C:\TestRoot\packages\FooPackage.1.0\lib\ja-JP\FooPackage.resources.dll", @"C:\TestRoot\bin\ja-JP\FooPackage.resources.dll" },
                };

                Assert.Equal<Dictionary<string, string>>(expected, files);
            }

            [Fact]
            public void FileDoesNotExist()
            {
                var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(fs => fs.FileExists(@"C:\TestRoot\packages\DoesNotExist.1.0\lib\DoesNotExist.dll")).Returns(false);

                NuGetWebProjectSystem projSystem = new NuGetWebProjectSystem(@"C:\TestRoot", mockFileSystem.Object, Dependencies.AssemblyResolver);

                Assert.Throws<FileNotFoundException>(() => projSystem.GetReferenceFilesToAdd(@"TestRoot\packages\DoesNotExist.1.0\lib\DoesNotExist.dll"));
            }
        }

        public class IsSupportedFile
        {
            [Theory]
            [InlineData("app.config")]
            [InlineData("App.Config")]
            [InlineData("app.Debug.config")]
            public void ExcludeAnyAppConfig(string path)
            {
                NuGetWebProjectSystem projSystem = new NuGetWebProjectSystem("Dummy");

                Assert.False(projSystem.IsSupportedFile(path));
            }

            [Theory]
            [InlineData("web.config")]
            [InlineData("web.Release.config")]
            [InlineData("packages.config")]
            public void AcceptAnyNonAppConfig(string path)
            {
                NuGetWebProjectSystem projSystem = new NuGetWebProjectSystem("Dummy");

                Assert.True(projSystem.IsSupportedFile(path));
            }

            [Theory]
            [InlineData("Foo.cs")]
            [InlineData("App.cs")]
            [InlineData("Fizz.config.Buzz")]
            public void AcceptAnyNonConfig(string path)
            {
                NuGetWebProjectSystem projSystem = new NuGetWebProjectSystem("Dummy");

                Assert.True(projSystem.IsSupportedFile(path));
            }
        }

        public class ResolvePath
        {
            [Theory]
            [InlineData(@"Foo.cs",          @"App_Code\Foo.cs")]
            [InlineData(@"App_Code\Foo.cs", @"App_Code\Foo.cs")]
            [InlineData(@"Foo.aspx.cs", @"Foo.aspx.cs")]
            [InlineData(@"Foo.foo.cs", @"App_Code\Foo.foo.cs")]
            [InlineData(@"Foo.aspx.garbage", @"Foo.aspx.garbage")]
            [InlineData(@"App_Start\Foo.cs", @"App_Start\Foo.cs")]
            [InlineData(@"Bar\Foo.cs",      @"App_Code\Bar\Foo.cs")]
            public void ResolvePaths(string input, string expected)
            {
                var projSystem = new NuGetWebProjectSystem("Dummy");
                Assert.Equal<string>(expected, projSystem.ResolvePath(input));
            }
        }

        public class AddFrameworkReference
        {
            [Fact]
            public void ThrowIfNotGACAssembly()
            {
                var projSystem = new NuGetWebProjectSystem("Dummy");

                Assert.Throws<InvalidOperationException>(() => projSystem.AddFrameworkReference("DoesNotExist"));
            }
        }

        public class AddReferencesToConfig
        {
            [Theory]
            [InlineData(@"<configuration><system.web><compilation><assemblies></assemblies></compilation></system.web></configuration>")]
            [InlineData(@"<configuration><system.web><compilation></compilation></system.web></configuration>")]
            public void AddOnlyAssembly(string originalWebConfig)
            {
                // arrange
                string finalWebConfig = "";
                var mockFileSystem = new Mock<global::NuGet.IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(fs => fs.FileExists("web.config")).Returns(true);
                mockFileSystem.Setup(fs => fs.Root).Returns("");
                mockFileSystem.Setup(fs => fs.OpenFile("web.config")).Returns(new MemoryStream(System.Text.Encoding.Default.GetBytes(originalWebConfig)));
                // and we need to intercept the result when it is saved
                mockFileSystem.Setup(fs => fs.AddFile("web.config", It.IsAny<Stream>()))
                    .Callback<string, Stream>((wc, stream) =>
                    {
                        finalWebConfig = new StreamReader(stream).ReadToEnd();
                    });

                // act
                NuGetWebProjectSystem.AddReferencesToConfig(mockFileSystem.Object, "FooAssembly");

                // assert
                string expected = @"<configuration><system.web><compilation><assemblies><add assembly=""FooAssembly"" /></assemblies></compilation></system.web></configuration>";
                Assert.Equal<string>(expected, finalWebConfig);
            }

            [Theory]
            [InlineData(@"<configuration><system.web><compilation><assemblies><add assembly=""BarAssembly"" /></assemblies></compilation></system.web></configuration>")]
            public void AddToExistingAssemblies(string originalWebConfig)
            {
                // arrange
                string finalWebConfig = "";
                var mockFileSystem = new Mock<global::NuGet.IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(fs => fs.FileExists("web.config")).Returns(true);
                mockFileSystem.Setup(fs => fs.Root).Returns("");
                mockFileSystem.Setup(fs => fs.OpenFile("web.config")).Returns(new MemoryStream(System.Text.Encoding.Default.GetBytes(originalWebConfig)));
                // and we need to intercept the result when it is saved
                mockFileSystem.Setup(fs => fs.AddFile("web.config", It.IsAny<Stream>()))
                    .Callback<string, Stream>((wc, stream) =>
                    {
                        finalWebConfig = new StreamReader(stream).ReadToEnd();
                    });

                // act
                NuGetWebProjectSystem.AddReferencesToConfig(mockFileSystem.Object, "FooAssembly");

                // assert
                string expected = @"<configuration><system.web><compilation><assemblies><add assembly=""FooAssembly"" /><add assembly=""BarAssembly"" /></assemblies></compilation></system.web></configuration>";
                Assert.Equal<string>(expected, finalWebConfig);
            }

            [Fact]
            public void NoChangeIfAssemblyExists()
            {
                //arrange
                string configContent = @"<configuration><system.web><compilation><assemblies><add assembly=""FooAssembly"" /></assemblies></compilation></system.web></configuration>";
                var mockFileSystem = new Mock<global::NuGet.IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(fs => fs.FileExists("web.config")).Returns(true);
                mockFileSystem.Setup(fs => fs.Root).Returns("");
                mockFileSystem.Setup(fs => fs.OpenFile("web.config")).Returns(new MemoryStream(System.Text.Encoding.Default.GetBytes(configContent)));

                // act
                // assert: Moq's strict behavior will verify AddFile is not called
                NuGetWebProjectSystem.AddReferencesToConfig(mockFileSystem.Object, "FooAssembly");
            }

            [Fact]
            public void InsertBeforeClearTag()
            {
                //arrange
                string configContent = @"<configuration><system.web><compilation><assemblies><clear /></assemblies></compilation></system.web></configuration>";
                string finalWebConfig = "";
                var mockFileSystem = new Mock<global::NuGet.IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(fs => fs.FileExists("web.config")).Returns(true);
                mockFileSystem.Setup(fs => fs.Root).Returns("");
                mockFileSystem.Setup(fs => fs.OpenFile("web.config")).Returns(new MemoryStream(System.Text.Encoding.Default.GetBytes(configContent)));
                // and we need to intercept the result when it is saved
                mockFileSystem.Setup(fs => fs.AddFile("web.config", It.IsAny<Stream>()))
                    .Callback<string, Stream>((wc, stream) =>
                    {
                        finalWebConfig = new StreamReader(stream).ReadToEnd();
                    });

                // act
                NuGetWebProjectSystem.AddReferencesToConfig(mockFileSystem.Object, "FooAssembly");

                // assert
                string expected = @"<configuration><system.web><compilation><assemblies><add assembly=""FooAssembly"" /><clear /></assemblies></compilation></system.web></configuration>";
                Assert.Equal<string>(expected, finalWebConfig);

            }

            [Fact]
            public void NoWebConfig()
            {
                string finalWebConfig = "";
                var mockFileSystem = new Mock<global::NuGet.IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(fs => fs.FileExists("web.config")).Returns(false);
                mockFileSystem.Setup(fs => fs.Root).Returns("");
                mockFileSystem.Setup(fs => fs.AddFile("web.config", It.Is<Stream>((s) => true)))
                    .Callback<string, Stream>((wc, stream) =>
                    {
                        finalWebConfig = new StreamReader(stream).ReadToEnd();
                    });

                NuGetWebProjectSystem.AddReferencesToConfig(mockFileSystem.Object, "FooAssembly");

                string expected = @"<configuration><system.web><compilation><assemblies><add assembly=""FooAssembly"" /></assemblies></compilation></system.web></configuration>";
                Assert.Equal<string>(expected, finalWebConfig);
            }

            [Fact]
            public void EmptyWebConfig()
            {
                //arrange
                string configContent = @"";
                var mockFileSystem = new Mock<global::NuGet.IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(fs => fs.FileExists("web.config")).Returns(true);
                mockFileSystem.Setup(fs => fs.Root).Returns("");
                mockFileSystem.Setup(fs => fs.OpenFile("web.config")).Returns(new MemoryStream(System.Text.Encoding.Unicode.GetBytes(configContent)));

                Assert.Throws<XmlException>(() => NuGetWebProjectSystem.AddReferencesToConfig(mockFileSystem.Object, "FooAssembly"));
            }
        }
    }
}
