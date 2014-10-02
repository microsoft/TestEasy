using System;
using Microsoft.Win32;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using TestEasy.Core;
using TestEasy.Core.Configuration;

namespace TestEasy.WebBrowser
{
    /// <summary>
    ///     Factory to create remote browsers returning selenium IWebDriver
    /// </summary>
    public class RemoteBrowserFactory : IBrowserFactory
    {
        /// <summary>
        ///     Create browser given browser type
        /// </summary>
        /// <param name="browserType"></param>
        /// <returns></returns>
        public IWebDriver CreateBrowser(BrowserType browserType)
        {
            if (AbstractionsLocator.Instance.RegistrySystem.GetRegistryKeyValue(Registry.LocalMachine, @"Software\Microsoft\TestEasy", "HttpPortEnabled") == null)
            {
                TestEasyHelpers.Firewall.AddPortToFirewall(80, "HttpPort");
                AbstractionsLocator.Instance.RegistrySystem.SetRegistryKeyValue(Registry.LocalMachine, @"Software\Microsoft\TestEasy", "HttpPortEnabled", 1);
            }

            var capability = DesiredCapabilities.HtmlUnitWithJavaScript();

            switch (browserType)
            {
                case BrowserType.Ie:
                    capability = DesiredCapabilities.InternetExplorer();
                    break;
                case BrowserType.Chrome:
                    capability = DesiredCapabilities.Chrome();
                    break;
                case BrowserType.Firefox:
                    capability = DesiredCapabilities.Firefox();
                    break;
                case BrowserType.Safari:
                    capability = DesiredCapabilities.Safari();
                    break;
                default: // <- case BrowserType.HtmlUnit or BrowserType.Default
                    return new RemoteWebDriver(capability);
            }

            return new RemoteWebDriver(new Uri(TestEasyConfig.Instance.Client.RemoteHubUrl), capability, TimeSpan.FromMinutes(5));                
        }
    }
}
