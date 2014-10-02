using System;
using System.Diagnostics;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Safari;
using TestEasy.Core;
using TestEasy.Core.Configuration;

namespace TestEasy.WebBrowser
{
    /// <summary>
    ///     Factory to create local browsers returning selenium IWebDriver
    /// </summary>
    public class LocalBrowserFactory : IBrowserFactory
    {
        private static Process _standaloneServerJar;

        /// <summary>
        ///     Create browser given browser type
        /// </summary>
        /// <param name="browserType"></param>
        /// <returns></returns>
        public IWebDriver CreateBrowser(BrowserType browserType)
        {
            IWebDriver result;

            switch (browserType)
            {
                case BrowserType.Ie:
                    {
                        TestEasyHelpers.Tools.DownloadTool("IEDriverServer.exe", addToFirewall: true);

                        result = new InternetExplorerDriver(
                            Environment.ExpandEnvironmentVariables(TestEasyConfig.Instance.Tools.DefaultLocalToolsPath), 
                            new InternetExplorerOptions { IntroduceInstabilityByIgnoringProtectedModeSettings = true }, 
                            TimeSpan.FromMinutes(5));
                    }
                    break;
                case BrowserType.Chrome:
                    {
                        TestEasyHelpers.Tools.DownloadTool("ChromeDriver.exe", addToFirewall: true);
                        var options = new ChromeOptions();
                        options.AddArgument("--test-type");
                        result = new ChromeDriver(Environment.ExpandEnvironmentVariables(TestEasyConfig.Instance.Tools.DefaultLocalToolsPath), 
                            options,
                            TimeSpan.FromMinutes(5));
                    }
                    break;
                case BrowserType.Firefox:
                    result = new FirefoxDriver();
                    break;
                case BrowserType.Safari:
                    result = new SafariDriver();
                    break;

                default: // <- case BrowserType.HtmlUnit or BrowserType.Default
                    {
                        var capability = DesiredCapabilities.HtmlUnitWithJavaScript();

                        try
                        {                            
                            result = new RemoteWebDriver(capability);
                        }
                        catch
                        {
                            try
                            {
                                // if selenium standalone server is not running we should see exception
                                if (_standaloneServerJar == null)
                                {
                                    var serverSingletonPath = TestEasyHelpers.Tools.DownloadTool("selenium-server-standalone.jar");
                                    var startInfo = new ProcessStartInfo(serverSingletonPath, "");
                                    _standaloneServerJar = Process.Start(startInfo);
                                }

                                result = new RemoteWebDriver(capability);
                            }
                            catch (Exception e)
                            {
                                throw new Exception("Could not initialize HtmlUnit WebDriver. Make sure that latest Java runtime is installed on you machine and selenium-server-standalone.jar is running. Java runtime environment can be downloaded from http://java.com/en/download/index.jsp or from TestEasy tools path.", e);
                            }
                        }
                    }
                    break;
            }

            return result;
        }
    }
}
