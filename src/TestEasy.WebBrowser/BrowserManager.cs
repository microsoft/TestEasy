using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using OpenQA.Selenium;
using TestEasy.Core.Configuration;

namespace TestEasy.WebBrowser
{
    /// <summary>
    ///     Browser manager, provides APIs for managing multiple browsers live cycle
    /// </summary>
    public class BrowserManager : IDisposable
    {
        private List<IWebDriver> _browsers;
        private readonly IBrowserFactory _localBrowserFactory;
        private readonly IBrowserFactory _remoteBrowserFactory;

        /// <summary>
        ///     List of currently active browsers
        /// </summary>
        public List<IWebDriver> Browsers
        {
            get { return _browsers; }
        }

        /// <summary>
        ///     ctor
        /// </summary>
        public BrowserManager()
        {
            _browsers = new List<IWebDriver>();
            _localBrowserFactory = new LocalBrowserFactory();
            _remoteBrowserFactory = new RemoteBrowserFactory();
        }

        /// <summary>
        ///     Create a new browser for given type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="remote"></param>
        /// <returns></returns>
        public IWebDriver CreateBrowser(BrowserType type = BrowserType.Default, bool? remote = null)
        {
            if (type == BrowserType.Default)
            {
                // by default try to use config if type not specified
                type = (BrowserType)Enum.Parse(typeof(BrowserType), TestEasyConfig.Instance.Client.Type);
            }

            var remoteValue = remote.HasValue ? remote.Value : TestEasyConfig.Instance.Client.Remote;
            var browser = remoteValue ? _remoteBrowserFactory.CreateBrowser(type) : _localBrowserFactory.CreateBrowser(type);
            
            if (browser != null)
            {
                _browsers.Add(browser);
            }

            return browser;
        }
        
        /// <summary>
        ///     Stop all browsers and close their windows
        /// </summary>
        public void KillAllBrowsers()
        {
            foreach (var b in _browsers)
            {
                if (b != null)
                {
                    b.Quit();
                }
            }

            // now kill all browser host processes
            var processesToKill = new []
                {
                    "ChromeDriver",
                    "conhost",
                    "IEDriverServer"
                };

            var processes =  Process.GetProcesses();
            foreach (var process in processes)
            {
                if (processesToKill.Any(p => p.Equals(process.ProcessName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    try
                    {
                        process.Close();
                    }
                    catch
                    {
                        // TODO do nothing for now, later add it to log or trace?
                    }
                }
            }
        }


        // dispose pattern
        ~BrowserManager()
        {
           Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing && _browsers != null)
            {
                // close browsers associated with this class
                KillAllBrowsers();
                _browsers = null;
            }
        }
    }
}
