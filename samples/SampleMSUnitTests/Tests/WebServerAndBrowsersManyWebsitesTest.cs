using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using TestEasy.Core;
using TestEasy.WebBrowser;
using TestEasy.WebServer;

namespace SampleMSUnitTests.Tests
{
    /// <summary>
    /// Note:
    ///     This sample test class contains TestMethods that need different websites to be deployed
    ///     to web server. Thus each of them have "Arrange" part that initializes Server, Application 
    ///     and VirtualPath specific to the test. After that workflow is the same as in other tests, 
    ///     just Browse to a url and do some DOM manipulations.
    /// </summary>
    
    [TestClass]
    [DeploymentItem("testsuite.config")]
    [DeploymentItem("SampleWebSites")]
    public class WebServerAndBrowsersManyWebsitesTest : WebTestcase
    {
        [TestMethod]
        [TestCategory("BVT")]
        public void DifferentWayToFindDomElementTest()
        {
            // Arrange
            const string webSiteName = "TestEasyWAP";

            var server = WebServer.Create();
            var appInfo = server.CreateWebApplication(webSiteName);
            server.DeployWebApplication(appInfo.Name, new List<DeploymentItem>
                {
                    new DeploymentItem { Type = DeploymentItemType.Directory, Path = webSiteName }
                });
            server.BuildWebApplication(webSiteName);

            using (var browserManager = new BrowserManager())
            {
                var browser = browserManager.CreateBrowser();
                // Act
                browser.Navigate().GoToUrl(appInfo.VirtualPath + "/SamplePostWebForm.aspx");

                // By xpath
                browser.FindElement(By.XPath("//input[@name='Button1']")).Click();
                browser.WaitForPageLoaded();

                // By Id
                browser.FindElement(By.Id("Button1")).Click();
                browser.WaitForPageLoaded();

                // By css selector
                browser.FindElement(By.CssSelector("#Button1")).Click();
                browser.WaitForPageLoaded();

                // Assert
                Assert.AreEqual("[Clicked!]", browser.FindElement(By.Id("TextBox1")).GetAttribute("value"));
            }
        }

        [TestMethod]
        [TestCategory("BVT")]
        public void DeployNavigateAndFindDomElementTest()
        {
            // Arrange
            const string webSiteName = "TestEasyWebSite";

            var server = WebServer.Create();
            var appInfo = server.CreateWebApplication(webSiteName);
            server.DeployWebApplication(appInfo.Name, new List<DeploymentItem>
                {
                    new DeploymentItem { Type = DeploymentItemType.Directory, Path = webSiteName }
                });

            using (var browserManager = new BrowserManager())
            {
                var browser = browserManager.CreateBrowser();

                // Act
                browser.Navigate().GoToUrl(appInfo.VirtualPath + "/default.html");

                // Assert
                Assert.AreEqual("[This is my label]", browser.FindElement(By.Id("myLabel")).Text);
            }
        }

        [TestMethod]
        [TestCategory("BVT")]
        public void DeployNavigateAndUploadFileTest()
        {
            // Arrange
            const string webSiteName = "TestEasyWAP";

            var server = WebServer.Create();
            var appInfo = server.CreateWebApplication(webSiteName);
            server.DeployWebApplication(appInfo.Name, new List<DeploymentItem>
                {
                    new DeploymentItem { Type = DeploymentItemType.Directory, Path = webSiteName }
                });
            server.BuildWebApplication(webSiteName);

            var fileToUpload = @"c:\temp\tempfile.txt";
            if (!File.Exists(fileToUpload))
            {
                if (!Directory.Exists(@"c:\temp"))
                {
                    Directory.CreateDirectory(@"c:\temp");
                }

                File.WriteAllText(fileToUpload, "some content");
            }

            using (var browserManager = new BrowserManager())
            {
                var browser = browserManager.CreateBrowser();

                // Act
                browser.Navigate().GoToUrl(appInfo.VirtualPath + "/SamplePostWebForm.aspx");

                browser.FindElement(By.Id("fileUpload")).SendKeys(fileToUpload);
                browser.FindElement(By.XPath("//input[@name='Button1']")).Click();

                browser.WaitForPageLoaded();

                // Assert
                Assert.AreEqual("[Clicked!]", browser.FindElement(By.Id("TextBox1")).GetAttribute("value"));
            }
        }
    }
}
