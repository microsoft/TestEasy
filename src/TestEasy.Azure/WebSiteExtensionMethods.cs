using WAML = Microsoft.WindowsAzure.Management;

namespace TestEasy.Azure
{
    /// <summary>
    ///     Web site extensions
    /// </summary>
    public static class WebSiteExtensionMethods
    {
        public static string GetUrl(this WAML.WebSites.Models.WebSite website)
        { 
            return website.HostNames.Count > 0 ? "http://" + website.HostNames[0] : "";
        }
    }
}
