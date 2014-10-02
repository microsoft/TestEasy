using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.Administration;
using OpenQA.Selenium;
using TestEasy.Server;
using TestEasy.Web;

namespace MSTestTemplate
{
    [TestClass]
    [DeploymentItem("testsuite.config")]
    [DeploymentItem("WebSites")]
    public class SampleTest : WebTestcase
    {
        const string WebSiteName = "MyTestWebSite";

        protected static WebServer Server { get; set; }
        protected static string VirtualPath { get; set; }
        protected static Application Application { get; set; }

        public SampleTest()
        {
            if (Server == null)
            {
                Server = CreateWebServer();

                Application = Server.CreateWebApplication(WebSiteName);
                Application.Deploy(WebSiteName);

                VirtualPath = Server.GetApplicationVirtualPath(Application);
            }
        }

        [TestMethod]
        public void SampleMsTest()
        {
            // Act
            Browser.Navigate().GoToUrl(VirtualPath + "/default.html");

            // Assert
            Assert.AreEqual("[This is my label]", Browser.FindElement(By.Id("myLabel")).Text);
        }
    }
}
