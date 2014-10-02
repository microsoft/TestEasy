using System;
using System.Net;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace TestEasy.WebBrowser
{
    /// <summary>
    ///     Extension for selenium IWebDriver
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        ///     Wait for ajax element to appear in the DOM
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="byElement"></param>
        public static void WaitForAjaxElement(this IWebDriver driver, By byElement)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait.Until(x => x.FindElement(byElement));
        }

        /// <summary>
        ///     Wait for ajax element in the DOM with timeout
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="byElement"></param>
        /// <param name="timeoutSeconds"></param>
        public static void WaitForAjaxElement(this IWebDriver driver, By byElement, double timeoutSeconds)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
            wait.Until(x => x.FindElement(byElement));
        }

        /// <summary>
        ///     Wait until element is actually displayed in the browser
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="webElement"></param>
        public static void WaitForWebElementDisplayed(this IWebDriver driver, IWebElement webElement)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            WaitForWebElementDisplayed(wait, webElement);
        }

        /// <summary>
        ///     Wait until element is enabled in the DOM
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="webElement"></param>
        public static void WaitForWebElementEnabled(this IWebDriver driver, IWebElement webElement)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            WaitForWebElementEnabled(wait, webElement);
        }

        /// <summary>
        ///     Wait until element is actually displayed in the DOM
        /// </summary>
        /// <param name="wait"></param>
        /// <param name="webElement"></param>
        public static void WaitForWebElementDisplayed(IWait<IWebDriver> wait, IWebElement webElement)
        {
            wait.Until(x => webElement.Displayed);
        }

        /// <summary>
        ///     Wait until element is enabled
        /// </summary>
        /// <param name="wait"></param>
        /// <param name="webElement"></param>
        public static void WaitForWebElementEnabled(IWait<IWebDriver> wait, IWebElement webElement)
        {
            wait.Until(x => webElement.Enabled);
        }

        /// <summary>
        ///     Wait until page is loaded 
        /// </summary>
        /// <param name="driver"></param>
        public static void WaitForPageLoaded(this IWebDriver driver)
        {
            var javaScriptExecuor = driver as IJavaScriptExecutor;
            if (javaScriptExecuor == null)
            {
                throw new Exception("The type of WebDriver that you use does not support java script execution and WaitForPageLoaded can not be used.");
            }

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait.Until(x => ((IJavaScriptExecutor)x).ExecuteScript("return document.readyState").Equals("complete"));
        } 

        /// <summary>
        ///     Return a status code after requesting given URL
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static HttpStatusCode GetHttpStatusCode(this IWebDriver driver, string url)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.AllowAutoRedirect = false;

            var response = (HttpWebResponse)webRequest.GetResponse();

            return response.StatusCode;
        }

        /// <summary>
        ///     Navigate to a page where timeout exception is expected (like pages with eternal javascript loops etc)
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="url"></param>
        /// <param name="timeout"></param>
        public static void SafeGoToUrlWithTimeout(this IWebDriver driver, string url, TimeSpan timeout)
        {
            // For pages that have eternal javascript connections, some browsers like IE
            // never return the call Navigate().GoToUrl(xxx) and just hang there. To work that around we
            // set small timeout and catch exception. After that driver should be able to work with DOM already.
            try
            {
                driver.Manage().Timeouts().SetPageLoadTimeout(timeout);
                driver.Navigate().GoToUrl(url);
            }
            catch (Exception)
            {
            }

            // set back to default
            driver.Manage().Timeouts().SetPageLoadTimeout(TimeSpan.FromSeconds(30));
        }

        /// <summary>
        ///     Click on an element when timeout exception is expected
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="by"></param>
        /// <param name="timeout"></param>
        public static void SafeElementClickWithTimeout(this IWebDriver driver, By by, TimeSpan timeout)
        {
            // For pages that have eternal javascript connections, some browsers like IE
            // never return the call Navigate().GoToUrl(xxx) and just hang there. To work that around we
            // set small timeout and catch exception. After that driver should be able to work with DOM already.
            try
            {
                driver.Manage().Timeouts().SetPageLoadTimeout(timeout);
                driver.FindElement(by).Click();
            }
            catch (Exception)
            {
            }

            // set back to default
            driver.Manage().Timeouts().SetPageLoadTimeout(TimeSpan.FromSeconds(30));
        }
    }
}
