using System.Collections.Generic;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using TestEasy.Core;
using TestEasy.WebBrowser;
using TestEasy.WebServer;

namespace SampleMSUnitTests.Tests
{
    /// <summary>
    /// Note:
    ///     This test class shows an example on how to create multiple tests reusing same website, 
    ///     that was deployed only once when first test was executing. This setup is also safe for
    ///     simultaneous tests execution. Mstests or xunit tests may execute simultaneously and thus 
    ///     constructor would be called for each thread, which could lead to multiple deployments of 
    ///     the same website. Thats why we define Server, Application and VirtualPath properties as 
    ///     static here, to make sure that initialization would happen only once and all tests would 
    ///     work with the ame instance of the website.
    /// </summary>
    
    [TestClass]
    [DeploymentItem("testsuite.config")]
    [DeploymentItem("SampleWebSites")]
    public class WebServerAndBrowsersSngleWebsitesTest : WebTestcase
    {
        const string WebSiteName = "TestEasyWebSite";

        protected static WebServer Server { get; set; }
        protected static WebApplicationInfo AppInfo{ get; set; }

        public WebServerAndBrowsersSngleWebsitesTest()
        {
            if (Server == null)
            {
                Server = WebServer.Create();

                AppInfo = Server.CreateWebApplication(WebSiteName);
                Server.DeployWebApplication(AppInfo.Name, new List<DeploymentItem>
                {
                    new DeploymentItem { Type = DeploymentItemType.Directory, Path = WebSiteName }
                });
            }
        }

        [TestMethod]
        [TestCategory("BVT")]
        public void NavigateAndFindDomElementTest()
        {
            using (var browserManager = new BrowserManager())
            {
                var browser = browserManager.CreateBrowser();

                // Act
                browser.Navigate().GoToUrl(AppInfo.VirtualPath + "/default.html");

                // Assert
                Assert.AreEqual("[This is my label]", browser.FindElement(By.Id("myLabel")).Text);
            }
        }

        [TestMethod]
        [TestCategory("BVT")]
        public void NavigateAndButtonClickTest()
        {
            using (var browserManager = new BrowserManager())
            {
                var browser = browserManager.CreateBrowser();

                // Act
                browser.Navigate().GoToUrl(AppInfo.VirtualPath + "/MySampleWebForm.aspx");

                // By Id
                browser.FindElement(By.Id("Button1")).Click();
                browser.WaitForPageLoaded();

                // Assert
                Assert.AreEqual("[Clicked!]", browser.FindElement(By.Id("TextBox1")).GetAttribute("value"));
            }
        }

        [TestMethod]
        [TestCategory("BVT")]
        public void GetStatusCodeTest()
        {
            using (var browserManager = new BrowserManager())
            {
                var browser = browserManager.CreateBrowser();

                // Act
                var httpStatusCode = browser.GetHttpStatusCode(AppInfo.VirtualPath + "/default.html");

                // Assert
                Assert.AreEqual(HttpStatusCode.OK, httpStatusCode);
            }
        }
    }
}
