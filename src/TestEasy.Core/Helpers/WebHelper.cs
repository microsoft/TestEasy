using TestEasy.Core.Abstractions;

namespace TestEasy.Core.Helpers
{
    /// <summary>
    ///     Helper APIs to deal with web requests
    /// </summary>
    public class WebHelper
    {
        internal readonly IWebRequestor WebRequestor;

        /// <summary>
        ///     ctor
        /// </summary>
        public WebHelper()
            :this(AbstractionsLocator.Instance.WebRequestor)
        {            
        }

        internal WebHelper(IWebRequestor webRequestor)
        {
            WebRequestor = webRequestor;
        }

        /// <summary>
        ///     Pings a URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public bool PingUrl(string url)
        {
            return WebRequestor.PingUrl(url);
        }
    }
}
