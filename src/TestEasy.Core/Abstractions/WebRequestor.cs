using System.Net;

namespace TestEasy.Core.Abstractions
{
    /// <summary>
    ///     Abstraction for making web requests
    /// </summary>
    public class WebRequestor: IWebRequestor
    {
        public bool PingUrl(string url)
        {
            TestEasyLog.Instance.Info(string.Format("Pinging URL: {0}", url));
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 15000;
            request.Method = "HEAD"; // we don't download content
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    TestEasyLog.Instance.Info(string.Format("Ping result: HTTP{0}", (int)response.StatusCode));
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException e)
            {
                TestEasyLog.Instance.Info(string.Format("Ping result failed with error: {0}", e.Status.ToString()));

                return false;
            }            
        }
    }
}
