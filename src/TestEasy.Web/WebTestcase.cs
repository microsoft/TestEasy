using System;
using OpenQA.Selenium;
using TestEasy.Client;
using TestEasy.Core;
using TestEasy.Core.Configuration;
using TestEasy.Server;

namespace TestEasy.Web
{
    public class WebTestcase
    {
        protected TestEasyConfig TestEasyConfig
        {
            get { return TestEasyConfig.Instance; }
        }

        private BrowserManager _browserManager;
        public IWebDriver Browser
        {
            get
            {
                if (_browserManager == null)
                {
                    var browserType = (BrowserType)Enum.Parse(typeof(BrowserType), TestEasyConfig.Client.Type);
                    _browserManager = new BrowserManager();
                    _browserManager.CreateBrowser(browserType, TestEasyConfig.Client.Remote);
                }

                return _browserManager.Browsers[0];
            }
        }

        public WebServer CreateWebServer(string websiteName = "")
        {
            var webServerType = (WebServerType)Enum.Parse(typeof(WebServerType), TestEasyConfig.WebServer.Type);

            if (TestEasyConfig.Client.Remote && webServerType == WebServerType.IISExpress)
            {
                throw new NotSupportedException("For tests using remote browsers, IISExpress web server type is not supported at this moment.");
            }

            var settings = new WebServerSettings
            {
                HostName = TestEasyConfig.Client.Remote ? Environment.MachineName : "",
                RootPhysicalPath = (webServerType == WebServerType.IISExpress)
                    ? TestEasyUtils.GetUniqueTempPath(websiteName)
                    : ""
            };

            return WebServer.Create(webServerType, settings);
        }
    }
}
