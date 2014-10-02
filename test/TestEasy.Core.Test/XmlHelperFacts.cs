using System;
using System.IO;
using Moq;
using TestEasy.Core.Abstractions;
using TestEasy.Core.Helpers;
using Xunit;
using Xunit.Extensions;
using System.Xml.XPath;
using System.Xml;

namespace TestEasy.Core.Test
{
    public class XmlHelperFacts
    {
        public class MergeXml
        {
            [Theory]
            [InlineData("", "", null, "")]
            [InlineData("some xml", "", null, "some xml")]
            [InlineData("<configuration></configuration>", "<configuration></configuration>", null, "<configuration></configuration>")]
            [InlineData("<configuration></configuration>", "<some></some>", null, "<configuration><some></some></configuration>")]
            [InlineData("<configuration><section1></section1></configuration>", "<configuration><section2></section2></configuration>", null, "<configuration><section1></section1><section2></section2></configuration>")]
            [InlineData("<configuration><section1 name=\"xxx\"></section1></configuration>",
                "<configuration><section1 name=\"yyy\" key=\"mykey\"></section1><section2></section2></configuration>", 
                null, 
                "<configuration><section1 name=\"yyy\" key=\"mykey\"></section1><section2></section2></configuration>")]
            [InlineData("<configuration><section1 name=\"xxx\"><subsection1></subsection1></section1></configuration>",
                "<configuration><section1 name=\"yyy\" key=\"mykey\"><subsection1></subsection1><subsection2 name=\"aaa\"></subsection2></section1><section2></section2></configuration>",
                null,
                "<configuration><section1 name=\"yyy\" key=\"mykey\"><subsection1></subsection1><subsection2 name=\"aaa\"></subsection2></section1><section2></section2></configuration>")]
            [InlineData("<configuration><section1 name=\"xxx\"><add></add></section1></configuration>",
                "<configuration><section1 name=\"yyy\" key=\"mykey\"><add></add><add name=\"aaa\"></add></section1><section2></section2></configuration>",
                null,
                "<configuration><section1 name=\"yyy\" key=\"mykey\"><add></add><add></add><add name=\"aaa\"></add></section1><section2></section2></configuration>")]
            [InlineData("<configuration><section1 name=\"xxx\"><user></user></section1></configuration>",
                 "<configuration><section1 name=\"yyy\" key=\"mykey\"><user></user><user name=\"aaa\"></user></section1><section2></section2></configuration>",
                 new [] {"user"},
                 "<configuration><section1 name=\"yyy\" key=\"mykey\"><user></user><user></user><user name=\"aaa\"></user></section1><section2></section2></configuration>")]
            public void VerifyMergeStringXml(string sourceXml, string overrideXml, string[] collectionElementNames,
                                    string expectedXml)
            {
                // Arrange 
                // Act 
                var xmlHelper = new XmlHelper();
                var actualXml = xmlHelper.MergeXml(sourceXml, overrideXml, collectionElementNames);

                // Assert
                Assert.Equal(expectedXml, actualXml);
            }

            [Fact]
            public void WhenMergeFile_IfSourceNull_ShouldThrow()
            {
                // Arrange
                var xmlHelper = new XmlHelper();

                // Act       
                // Assert
                Assert.Throws<ArgumentNullException>(() => xmlHelper.MergeXmlFiles(null, null));
            }

            [Fact]
            public void WhenMergeFile_IfOverrideNull_ShouldThrow()
            {
                // Arrange
                var xmlHelper = new XmlHelper();

                // Act       
                // Assert
                Assert.Throws<ArgumentNullException>(() => xmlHelper.MergeXmlFiles(@"somefilepath", null));
            }
            [Fact]
            public void WhenMergeFile_IfSourceNotExist_ShouldThrow()
            {
                // Arrange
                var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(m => m.FileExists(@"somefilepath")).Returns(false);

                var xmlHelper = new XmlHelper(mockFileSystem.Object);

                // Act       
                // Assert
                var exception = Assert.Throws<FileNotFoundException>(
                        () => xmlHelper.MergeXmlFiles(@"somefilepath", @"overridefilepath")); 
                Assert.Equal("File does not exist 'somefilepath'.", exception.Message);
            }

            [Fact]
            public void WhenMergeFile_IfOverrideNotExist_ShouldThrow()
            {
                // Arrange
                var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
                mockFileSystem.Setup(m => m.FileExists(@"somefilepath")).Returns(true);
                mockFileSystem.Setup(m => m.FileExists(@"overridefilepath")).Returns(false);

                var xmlHelper = new XmlHelper(mockFileSystem.Object);

                // Act       
                // Assert
                var exception = Assert.Throws<FileNotFoundException>(
                        () => xmlHelper.MergeXmlFiles(@"somefilepath", @"overridefilepath"));
                Assert.Equal("File does not exist 'overridefilepath'.", exception.Message);
            }
        }

        public class ContainsElement
        {
            [Theory]
            [InlineData("<configuration></configuration>", "configuration", true)]
            [InlineData("<configuration></configuration>", "/configuration", true)]
            [InlineData("<configuration><foo></foo></configuration>", "//foo", true)]
            [InlineData("<configuration><foo><bar /></foo></configuration>", "/configuration/foo/bar", true)]
            [InlineData("<configuration><fizz><buzz><Foo /></buzz></fizz></configuration>", "//foo", false)]
            [InlineData(@"<configuration><system.web><compilation><assemblies><add name=""Test"" /></assemblies></compilation></system.web></configuration>", @"/configuration/system.web/compilation/assemblies/add[@name=""Test""]", true)]
            [InlineData(@"<configuration><system.web><compilation><assemblies><add name=""Test, Version=1.0.0.0"" /></assemblies></compilation></system.web></configuration>", @"/configuration/system.web/compilation/assemblies/add[@name=""Test""]", false)]
            [InlineData(@"<configuration><connectionStrings><add name=""Test"" /></connectionStrings><system.web><compilation><assemblies><add name=""Test, Version=1.0.0.0"" /></assemblies></compilation></system.web></configuration>", @"/configuration/system.web/compilation/assemblies/add[@name=""Test""]", false)]
            public void VerifyContainsElement(string xml, string element, bool expected)
            {
                var xmlHelper = new XmlHelper();

                var result = xmlHelper.XmlContainsElement(xml, element);

                Assert.Equal(expected, result);
            }

            [Fact]
            public void FileIsNull_ShouldThrow()
            {
                var xmlHelper = new XmlHelper();

                Assert.Throws<ArgumentNullException>(() => xmlHelper.FileContainsElement(null, ""));
            }

            [Fact]
            public void FileDoesNotExist_ShouldThrow()
            {
                var xmlHelper = new XmlHelper();

                Assert.Throws<FileNotFoundException>(() => xmlHelper.FileContainsElement("dummy.xml", ""));
            }

            [Fact]
            public void XmlIsNull_ShouldThrow()
            {
                var xmlHelper = new XmlHelper();

                Assert.Throws<ArgumentNullException>(() => xmlHelper.XmlContainsElement(null, ""));
            }

            [Fact]
            public void XmlIsEmpty_ShouldThrow()
            {
                var xmlHelper = new XmlHelper();

                Assert.Throws<XmlException>(() => xmlHelper.XmlContainsElement("", ""));
            }

            [Fact]
            public void XmlIsInvalid_ShouldThrow()
            {
                var xmlHelper = new XmlHelper();

                Assert.Throws<XmlException>(() => xmlHelper.XmlContainsElement("<root>", ""));
            }


            [Fact]
            public void XpathQueryInvalid_ShouldThrow()
            {
                var xmlHelper = new XmlHelper();

                Assert.Throws<XPathException>(() => xmlHelper.XmlContainsElement("<root />", "////"));
            }

            [Fact]
            public void XpathQueryNull_ShouldThrow()
            {
                var xmlHelper = new XmlHelper();

                Assert.Throws<ArgumentNullException>(() => xmlHelper.XmlContainsElement("<root />", null));
            }

        }
    }
}
