using System.Web;
using Microsoft.WindowsAzure.Management.WebSites.Models;

namespace TestEasy.Azure
{
    /// <summary>
    ///     Publish profile extentions
    /// </summary>
    public static class PublishProfileExtensions
    {
        /// <summary>
        ///     Returns publish URL for profile
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public static string GetWebDeployUrl(this WebSiteGetPublishProfileResponse.PublishProfile profile)
        {
            return string.Format(AzureServiceConstants.AzureWebDeployUrl, profile.PublishUrl, HttpUtility.UrlEncode(profile.MSDeploySite));
        }
    }
}
