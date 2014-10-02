using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using TestEasy.Core;
using TestEasy.Core.Configuration;
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
    public class AdvancedWebServerAndClientTest
    {
        /// <summary>
        /// This example in details shows what is perferred way to create WebServer and Browser objects keeping 
        /// in mind configuration system. It is not necessary to do it if you base your test class on WebTestcase 
        /// class (from TestEasy.Web.dll), since it hides similar actions from you and you always have access to 
        ///     - CreateWebServer method
        ///     - Config property 
        ///     - Browser property
        /// 
        /// However depending on your test logic it is not required to inherrit from WebTestcase and in such
        /// cases you should use example below to initialize your web server and browsers.
        /// </summary>
        [TestMethod]
        [TestCategory("BVT")]
        public void CustomServerAndBrowserUsingConfigTest()
        {
            // Arrange
            const string webSiteName = "TestEasyWAP";

            // Create server using Config settings
            WebServerSettings settings = new WebServerSettings
            {
                HostName = TestEasyConfig.Instance.Client.Remote ? Environment.MachineName : "",
                RootPhysicalPath = (TestEasyConfig.Instance.WebServer.Type.Equals("IISExpress", StringComparison.InvariantCultureIgnoreCase))
                    ? TestEasyHelpers.Tools.GetUniqueTempPath(webSiteName)
                    : ""
            };

            WebServer server = WebServer.Create(TestEasyConfig.Instance.WebServer.Type, settings);
            WebApplicationInfo appInfo = server.CreateWebApplication(webSiteName);
            server.DeployWebApplication(appInfo.Name, new List<DeploymentItem>
                {
                    new DeploymentItem { Type = DeploymentItemType.Directory, Path = webSiteName }
                });
            server.BuildWebApplication(webSiteName);
            
            // create browser object using config settings
            using (var browserManager = new BrowserManager())
            {
                var browser = browserManager.CreateBrowser();

                // Act
                browser.Navigate().GoToUrl(appInfo.VirtualPath + "/SamplePostWebForm.aspx");

                browser.FindElement(By.XPath("//input[@name='Button1']")).Click();
                browser.WaitForPageLoaded();

                // Assert
                Assert.AreEqual("[Clicked!]", browser.FindElement(By.Id("TextBox1")).GetAttribute("value"));
            }
        }

        /// <summary>
        /// This method is simplified version of previous method and shows you how manually create Browsers and 
        /// WebServer objects using some custom settings.
        /// 
        ///  Note: Here we create WebServer using custom settings and don't read config TestEasyConfig.Instance.
        /// This is not preferred way, since we always want to have dependency on config to allow runtime test 
        /// settings manipulation.
        /// </summary>
        [TestMethod]
        [TestCategory("BVT")]
        public void CustomServerAndBrowserTest()
        {
            // Arrange
            const string webSiteName = "TestEasyWAP";

            // Create server object
            WebServerSettings settings = new WebServerSettings
            {
                HostName = Environment.MachineName, // machine anme or empty (in this case localhost will be used)
                RootPhysicalPath = TestEasyHelpers.Tools.GetUniqueTempPath(webSiteName) // needed only when web server type is IISExpress
            };

            WebServer server = WebServer.Create("IISExpress", settings);
            WebApplicationInfo appInfo = server.CreateWebApplication(webSiteName);
            server.DeployWebApplication(appInfo.Name, new List<DeploymentItem>
                {
                    new DeploymentItem { Type = DeploymentItemType.Directory, Path = webSiteName }
                });
            server.BuildWebApplication(webSiteName);

            // create browser object
            using (var browserManager = new BrowserManager())
            {
                var browser = browserManager.CreateBrowser(BrowserType.Ie, false/*, remote=true/false */);

                // Act
                browser.Navigate().GoToUrl(appInfo.VirtualPath + "/SamplePostWebForm.aspx");

                browser.FindElement(By.XPath("//input[@name='Button1']")).Click();
                browser.WaitForPageLoaded();

                // Assert
                Assert.AreEqual("[Clicked!]", browser.FindElement(By.Id("TextBox1")).GetAttribute("value"));
            }
        }

        /// <summary>
        /// This test demonstrates how you can create as many browser instances as you want and browse to 
        /// your sample web application. Please have a look at BrowserManager class that is responsible for
        /// browsers manipulation.
        /// 
        /// Note: Here we create WebServer using custom settings and don't read config TestEasyConfig.Instance.
        /// This is not preferred way, since we always want to have dependency on config to allow runtime test 
        /// settings manipulation.
        /// </summary>
        [TestMethod]
        [TestCategory("BVT")]
        public void MultipleBrowsersTest()
        {
            // Arrange
            const string webSiteName = "TestEasyWAP";

            // Create server object
            WebServerSettings settings = new WebServerSettings
            {
                HostName = Environment.MachineName, // machine anme or empty (in this case localhost will be used)
                RootPhysicalPath = TestEasyHelpers.Tools.GetUniqueTempPath(webSiteName) // needed only when web server type is IISExpress
            };

            WebServer server = WebServer.Create("IISExpress", settings);
            WebApplicationInfo appInfo = server.CreateWebApplication(webSiteName);
            server.DeployWebApplication(appInfo.Name, new List<DeploymentItem>
                {
                    new DeploymentItem { Type = DeploymentItemType.Directory, Path = webSiteName }
                });
            server.BuildWebApplication(webSiteName);

            // create browser object
            using (BrowserManager browserManager = new BrowserManager())
            {
                List<IWebDriver> myBrowsers = new List<IWebDriver>
                    {
                        browserManager.CreateBrowser(remote:false),
                        browserManager.CreateBrowser(remote:false),
                        browserManager.CreateBrowser(remote:false)
                    };

                foreach (var browser in myBrowsers)
                {
                    // Act
                    browser.Navigate().GoToUrl(appInfo.VirtualPath + "/SamplePostWebForm.aspx");

                    browser.FindElement(By.Id("Button1")).Click();
                    browser.WaitForPageLoaded();


                    // Assert
                    Assert.AreEqual("[Clicked!]", browser.FindElement(By.Id("TextBox1")).GetAttribute("value"));
                }
            }
        }

        [TestMethod]
        [TestCategory("BVT")]
        public void SpecifyingBrowserCompatibilityModeForIe()
        {
            // Arrange
            const string webSiteName = "TestEasyWebSite";

            // create Virtual Application and deploy files
            WebServer server = WebServer.Create();
            WebApplicationInfo appInfo = server.CreateWebApplication(webSiteName);
            server.DeployWebApplication(appInfo.Name, new List<DeploymentItem>
                {
                    new DeploymentItem { Type = DeploymentItemType.Directory, Path = webSiteName }
                });

            server.SetCustomHeaders(
                Path.Combine(appInfo.PhysicalPath, "web.config"), 
                new Dictionary<string, string> { { "X-UA-Compatible", " IE=8"} });

            // create browser object using config settings
            using (var browserManager = new BrowserManager())
            {
                var browser = browserManager.CreateBrowser(BrowserType.Ie, false);

                // Act
                browser.Navigate().GoToUrl(appInfo.VirtualPath + "/default.html");

                // Assert
                Assert.AreEqual("[This is my label]", browser.FindElement(By.Id("myLabel")).Text);
            }
        }
    }
}
