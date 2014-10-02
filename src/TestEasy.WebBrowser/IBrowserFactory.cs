using OpenQA.Selenium;
using TestEasy.Core.Configuration;

namespace TestEasy.WebBrowser
{
    public interface IBrowserFactory
    {
        IWebDriver CreateBrowser(BrowserType browserType);
    }
}
