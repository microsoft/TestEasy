using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.WindowsAzure.Management.WebSites;
using Microsoft.WindowsAzure.Management.WebSites.Models;
using TestEasy.Azure.Helpers;
using TestEasy.Core;
using TestEasy.Core.Helpers;

namespace TestEasy.Azure
{
    /// <summary>
    ///     Contains helper methods for managing Azure WebSites
    /// </summary>
    public class AzureWebSiteManager
    {
        private readonly string _webSpace;

        /// <summary>
        ///     Rest API client
        /// </summary>
        public IWebSiteManagementClient WebSiteManagementClient
        {
            get;
            private set;
        }

        internal AzureWebSiteManager(string webSpace = AzureServiceConstants.DefaultWebSpace)
        {            
            WebSiteManagementClient = new WebSiteManagementClient(Dependencies.Subscription.Credentials, new Uri(Dependencies.Subscription.CoreEndpointUrl));
            
            var webSpaces = WebSiteManagementClient.WebSpaces.ListAsync(new CancellationToken()).Result;
            if (!webSpaces.Any(s => s.Name.Equals(webSpace, StringComparison.InvariantCultureIgnoreCase)))
            {
                webSpace = webSpaces.First().Name;
            }

            _webSpace = webSpace;
        }

        /// <summary>
        ///     Create a website
        /// </summary>
        /// <param name="name"></param>
        /// <param name="connectionStringInfo"></param>
        /// <returns></returns>
        public WebSite CreateWebSite(string name = "", WebSiteUpdateConfigurationParameters.ConnectionStringInfo connectionStringInfo = null)
        {
            return CreateWebSite(_webSpace, new WebSiteCreateParameters
            {
                Name = name
            }, connectionStringInfo);
        }

        /// <summary>
        ///     Create a website
        /// </summary>
        /// <param name="webSpaceName"></param>
        /// <param name="siteSettings"></param>
        /// <param name="connectionStringInfo"></param>
        /// <returns></returns>
        public WebSite CreateWebSite(string webSpaceName, WebSiteCreateParameters siteSettings,
            WebSiteUpdateConfigurationParameters.ConnectionStringInfo connectionStringInfo = null)
        {
            if (siteSettings == null)
            {
                throw new ArgumentNullException("siteSettings");
            }

            siteSettings.Name = string.IsNullOrEmpty(siteSettings.Name) 
                ? Dependencies.TestResourcesCollector.GetUniqueWebSiteName() 
                : siteSettings.Name.ToLowerInvariant();
            
            if (siteSettings.HostNames == null || siteSettings.HostNames.Count == 0)
            {
                siteSettings.HostNames = new [] {siteSettings.Name + Dependencies.Subscription.DefaultWebSitesDomainName };
            }

            if (string.IsNullOrEmpty(siteSettings.WebSpaceName))
            {
                siteSettings.WebSpaceName = webSpaceName;
            }

            TestEasyLog.Instance.Info(string.Format("Creating web site '{0}'", siteSettings.Name));

            var createWebsiteResult = WebSiteManagementClient.WebSites.CreateAsync(webSpaceName,
                siteSettings,
                new CancellationToken()).Result;

            var newSite = createWebsiteResult.WebSite;
            Dependencies.TestResourcesCollector.Remember(AzureResourceType.WebSite, newSite.Name, newSite);

            if(connectionStringInfo != null)
            {
                var existingConfig = CreateWebSiteUpdateParameters(newSite.Name);
                var existingConnectionStrings = existingConfig.ConnectionStrings;
                if(existingConnectionStrings.All(cs => cs.Name != connectionStringInfo.Name))
                {
                    existingConnectionStrings.Add(connectionStringInfo);
                    UpdateWebSiteConfig(webSpaceName, newSite.Name, existingConfig);
                }
            }

            return newSite;
        }

        /// <summary>
        ///     Get website information
        /// </summary>
        /// <param name="siteName"></param>
        /// <returns></returns>
        public WebSite GetWebSite(string siteName)
        {
            return GetWebSite(_webSpace, siteName);
        }

        /// <summary>
        ///     Get website information
        /// </summary>
        /// <param name="webSpaceName"></param>
        /// <param name="siteName"></param>
        /// <returns></returns>
        public WebSite GetWebSite(string webSpaceName, string siteName)
        {
            TestEasyLog.Instance.Info(string.Format("Getting web site '{0}'", siteName));

            var allWebsites = WebSiteManagementClient.WebSpaces.ListWebSitesAsync(webSpaceName,
                new WebSiteListParameters { PropertiesToInclude = { "Name" } },
                new CancellationToken()).Result;
            var web = allWebsites.FirstOrDefault(w => w.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));

            return web;
        }

        /// <summary>
        ///     Get web site configuration settings
        /// </summary>
        /// <param name="siteName"></param>
        /// <returns></returns>
        public WebSiteGetConfigurationResponse GetWebSiteConfig(string siteName)
        {
            return GetWebSiteConfig(_webSpace, siteName);
        }

        /// <summary>
        ///     Get web site configuration settings
        /// </summary>
        /// <param name="webSpaceName"></param>
        /// <param name="siteName"></param>
        /// <returns></returns>
        public WebSiteGetConfigurationResponse GetWebSiteConfig(string webSpaceName, string siteName)
        {
            TestEasyLog.Instance.Info(string.Format("Getting web site config '{0}'", siteName));
            var rawResult = WebSiteManagementClient.WebSites.GetConfigurationAsync(webSpaceName, siteName, new CancellationToken()).Result;
            return rawResult;
        }

        /// <summary>
        ///     Create websie update parameters
        /// </summary>
        /// <param name="siteName"></param>
        /// <returns></returns>
        public WebSiteUpdateConfigurationParameters CreateWebSiteUpdateParameters(string siteName)
        {
            return CreateWebSiteUpdateParameters(_webSpace, siteName);
        }

        /// <summary>
        ///     Create websie update parameters
        /// </summary>
        /// <param name="webSpaceName"></param>
        /// <param name="siteName"></param>
        /// <returns></returns>
        public WebSiteUpdateConfigurationParameters CreateWebSiteUpdateParameters(string webSpaceName, string siteName)
        {
            return CreateWebSiteUpdateParameters(GetWebSiteConfig(webSpaceName, siteName));
        }

        /// <summary>
        ///     Create websie update parameters
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public WebSiteUpdateConfigurationParameters CreateWebSiteUpdateParameters(WebSiteGetConfigurationResponse r)
        {
            return new WebSiteUpdateConfigurationParameters
            {
                AppSettings = r.AppSettings,
                ConnectionStrings = r.ConnectionStrings.Select(csi => new WebSiteUpdateConfigurationParameters.ConnectionStringInfo
                {
                    ConnectionString = csi.ConnectionString,
                    Name = csi.Name,
                    Type = csi.Type,
                }).ToList(),
                DefaultDocuments = r.DefaultDocuments,
                DetailedErrorLoggingEnabled = r.DetailedErrorLoggingEnabled,
                HandlerMappings = r.HandlerMappings.Select(hm => new WebSiteUpdateConfigurationParameters.HandlerMapping
                {
                    Arguments = hm.Arguments,
                    Extension = hm.Extension,
                    ScriptProcessor = hm.ScriptProcessor,
                }).ToList(),
                HttpLoggingEnabled = r.HttpLoggingEnabled,
                Metadata = r.Metadata,
                NetFrameworkVersion = r.NetFrameworkVersion,
                NumberOfWorkers = r.NumberOfWorkers,
                PhpVersion = r.PhpVersion,
                PublishingPassword = r.PublishingPassword,
                PublishingUserName = r.PublishingUserName,
                RequestTracingEnabled = r.RequestTracingEnabled,
                ScmType = r.ScmType,
            };

        }

        /// <summary>
        ///     Update website
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="siteSettings"></param>
        public void UpdateWebSite(string siteName, WebSiteUpdateParameters siteSettings)
        {
            UpdateWebSite(_webSpace, siteName, siteSettings);
        }

        /// <summary>
        ///     Update website
        /// </summary>
        /// <param name="webSpaceName"></param>
        /// <param name="siteName"></param>
        /// <param name="siteSettings"></param>
        public void UpdateWebSite(string webSpaceName, string siteName, WebSiteUpdateParameters siteSettings)
        {
            TestEasyLog.Instance.Info(string.Format("Updating web site '{0}'", siteName));
            WebSiteManagementClient.WebSites.UpdateAsync(webSpaceName, siteName, siteSettings, new CancellationToken()).Wait();
        }

        /// <summary>
        ///     Update web site configuration
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="updateConfigParams"></param>
        public void UpdateWebSiteConfig(string siteName, WebSiteUpdateConfigurationParameters updateConfigParams)
        {
            UpdateWebSiteConfig(_webSpace, siteName, updateConfigParams);
        }

        /// <summary>
        ///     Update website configuration
        /// </summary>
        /// <param name="webSpaceName"></param>
        /// <param name="siteName"></param>
        /// <param name="updateConfigParams"></param>
        public void UpdateWebSiteConfig(string webSpaceName, string siteName, WebSiteUpdateConfigurationParameters updateConfigParams)
        {
            TestEasyLog.Instance.Info(string.Format("Updating web site config '{0}'", siteName));
            WebSiteManagementClient.WebSites.UpdateConfigurationAsync(webSpaceName, siteName, updateConfigParams, new CancellationToken()).Wait();
        }

        /// <summary>
        ///     Delete website
        /// </summary>
        /// <param name="siteName"></param>
        public void DeleteWebSite(string siteName)
        {
            DeleteWebSite(_webSpace, siteName);
        }

        /// <summary>
        ///     Delete website
        /// </summary>
        /// <param name="webSpaceName"></param>
        /// <param name="siteName"></param>
        public void DeleteWebSite(string webSpaceName, string siteName)
        {
            TestEasyLog.Instance.Info(string.Format("Deleting web site '{0}'", siteName));
            var result = WebSiteManagementClient.WebSites.DeleteAsync(webSpaceName, siteName, new WebSiteDeleteParameters(), new CancellationToken()).Result;
        }

        /// <summary>
        ///     Get publishing profiles for MSDeploy and Ftp
        /// </summary>
        /// <param name="siteName"></param>
        /// <returns></returns>
        public IList<WebSiteGetPublishProfileResponse.PublishProfile> GetPublishingProfiles(string siteName)
        {
            return GetPublishingProfiles(_webSpace, siteName);
        }

        /// <summary>
        ///     Get publishing profiles for MSDeploy and Ftp
        /// </summary>
        /// <param name="webSpaceName"></param>
        /// <param name="siteName"></param>
        /// <returns></returns>
        public IList<WebSiteGetPublishProfileResponse.PublishProfile> GetPublishingProfiles(string webSpaceName, string siteName)
        {
            TestEasyLog.Instance.Info(string.Format("Getting publishing profiles for web site '{0}'", siteName));
            var result = WebSiteManagementClient.WebSites.GetPublishProfileAsync(webSpaceName, siteName, new CancellationToken()).Result;
            return result.PublishProfiles;
        }

        /// <summary>
        ///     Get publishing profiles for MSDeploy and Ftp
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="publishMethod"></param>
        /// <returns></returns>
        public WebSiteGetPublishProfileResponse.PublishProfile GetPublishingProfile(string siteName, PublishMethod publishMethod = PublishMethod.MSDeploy)
        {
            return GetPublishingProfile(_webSpace, siteName, publishMethod);
        }

        /// <summary>
        ///     Get publishing profiles for MSDeploy and Ftp
        /// </summary>
        /// <param name="webSpaceName"></param>
        /// <param name="siteName"></param>
        /// <param name="publishMethod"></param>
        /// <returns></returns>
        public WebSiteGetPublishProfileResponse.PublishProfile GetPublishingProfile(string webSpaceName, string siteName, PublishMethod publishMethod = PublishMethod.MSDeploy)
        {
            TestEasyLog.Instance.Info(string.Format("Getting publishing profile for web site '{0}'", siteName));
            return GetPublishingProfiles(webSpaceName, siteName).FirstOrDefault(p => p.PublishMethod.Equals(publishMethod.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     Restart website
        /// </summary>
        /// <param name="siteName"></param>
        public void RestartWebSite(string siteName)
        {
            RestartWebSite(_webSpace, siteName);
        }

        /// <summary>
        ///     Restart website
        /// </summary>
        /// <param name="webSpaceName"></param>
        /// <param name="siteName"></param>
        public void RestartWebSite(string webSpaceName, string siteName)
        {
            TestEasyLog.Instance.Info(string.Format("Restarting web site '{0}'", siteName));
            WebSiteManagementClient.WebSites.RestartAsync(webSpaceName, siteName, new CancellationToken()).Wait();
        }

        /// <summary>
        ///     Check if website exists
        /// </summary>
        /// <param name="siteName"></param>
        /// <returns></returns>
        public bool WebSiteExists(string siteName)
        {
            return WebSiteExists(_webSpace, siteName);
        }

        /// <summary>
        ///     Check if website exists
        /// </summary>
        /// <param name="webSpaceName"></param>
        /// <param name="siteName"></param>
        /// <returns></returns>
        public bool WebSiteExists(string webSpaceName, string siteName)
        {
            TestEasyLog.Instance.Info(string.Format("Checking if website '{0}' exists", siteName));
            return GetWebSite(webSpaceName, siteName) != null;
        }

        /// <summary>
        ///     Publish website using MSDeploy or Ftp
        /// </summary>
        /// <param name="siteRoot"></param>
        /// <param name="siteName"></param>
        /// <param name="publishMethod"></param>
        /// <param name="siteRootRelativePath"></param>
        /// <param name="deleteExisting"></param>
        /// <param name="paramResolverFunc"></param>
        /// <returns></returns>
        public bool PublishWebSite(string siteRoot, string siteName, PublishMethod publishMethod = PublishMethod.MSDeploy, string siteRootRelativePath = "", bool deleteExisting = true, Func<string, string> paramResolverFunc = null)
        {
            if (!Directory.Exists(siteRoot))
            {
                throw new DirectoryNotFoundException(string.Format("Publishing error: site directory '{0}' does not exist.", siteRoot));
            }

            var result = true;
            TestEasyLog.Instance.Info(string.Format("Publishing web site '{0}'", siteName));

            var publishProfile = GetPublishingProfile(siteName, publishMethod);
            switch (publishMethod)
            {
                case PublishMethod.MSDeploy:
                    WebDeployHelper.DeployWebSite(siteRoot, siteName, publishProfile.GetWebDeployUrl(), publishProfile.UserName, publishProfile.UserPassword, deleteExisting, paramResolverFunc);
                    break;
                case PublishMethod.Ftp:
                    result = FtpHelper.Authorize(publishProfile.UserName, publishProfile.UserPassword)
                             .UploadDir(siteRoot, publishProfile.PublishUrl, siteRootRelativePath);
                    break;
                default:
                    throw new Exception(string.Format("Deployment method '{0}' is not supported.", publishMethod));
            }

            return result;
        }

        /// <summary>
        ///     Publish website using MSDeploy or Ftp
        /// </summary>
        /// <param name="file"></param>
        /// <param name="siteName"></param>
        /// <param name="publishMethod"></param>
        /// <param name="siteRootRelativePath"></param>
        /// <param name="deleteExisting"></param>
        /// <param name="paramResolverFunc"></param>
        /// <returns></returns>
        public bool PublishFile(string file, string siteName, PublishMethod publishMethod = PublishMethod.MSDeploy, string siteRootRelativePath = "", bool deleteExisting = true, Func<string, string> paramResolverFunc = null)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException(string.Format("Publishing error: file '{0}' does not exist.", file));
            }

            var result = true;
            var publishProfile = GetPublishingProfile(siteName, publishMethod);
            var fileRelativePath = siteName + "/";
            if (!string.IsNullOrEmpty(siteRootRelativePath))
            {
                fileRelativePath += (siteRootRelativePath + "/");
            }

            fileRelativePath += Path.GetFileName(file);

            TestEasyLog.Instance.Info(string.Format("Publishing file '{0}' to web site '{1}'", file, siteName));           

            switch (publishMethod)
            {
                case PublishMethod.MSDeploy:
                    WebDeployHelper.DeployFile(file, fileRelativePath, publishProfile.GetWebDeployUrl(), publishProfile.UserName, publishProfile.UserPassword, deleteExisting, paramResolverFunc);
                    break;
                case PublishMethod.Ftp:
                    result = FtpHelper.Authorize(publishProfile.UserName, publishProfile.UserPassword)
                             .UploadFile(file, Path.Combine(publishProfile.PublishUrl, fileRelativePath));
                    break;
                default:
                    throw new Exception(string.Format("Deployment method '{0}' is not supported.", publishMethod));
            }

            return result;
        }

        /// <summary>
        ///     Publish directory using MSDeploy or Ftp
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="siteName"></param>
        /// <param name="publishMethod"></param>
        /// <param name="siteRootRelativePath"></param>
        /// <param name="deleteExisting"></param>
        /// <param name="paramResolverFunc"></param>
        /// <returns></returns>
        public bool PublishDirectory(string directory, string siteName, PublishMethod publishMethod = PublishMethod.MSDeploy, string siteRootRelativePath = "",
                                     bool deleteExisting = true, Func<string, string> paramResolverFunc = null)
        {
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(string.Format("Publishing error: directory '{0}' does not exist.", directory));
            }

            var result = true;

            var levelUpRequested = 0;
            if (!string.IsNullOrEmpty(siteRootRelativePath))
            {
                // remove .., / and \ from relative path 
                siteRootRelativePath = siteRootRelativePath.Trim('\\').Trim('/');
                if (siteRootRelativePath.StartsWith(".."))
                {
                    while (siteRootRelativePath.StartsWith("..") || siteRootRelativePath.StartsWith("/") ||
                           siteRootRelativePath.StartsWith("\\"))
                    {
                        levelUpRequested++;
                        siteRootRelativePath = siteRootRelativePath.Trim('.').Trim('\\').Trim('/');
                    }

                    siteRootRelativePath = siteRootRelativePath.Trim('.');

                    if (levelUpRequested > 2)
                    {
                        levelUpRequested = 2;
                    }
                }
            }

            var publishProfile = GetPublishingProfile(siteName, publishMethod);

            switch (publishMethod)
            {
                case PublishMethod.MSDeploy:
                {
                    var dirRelativePath = siteName + "/";
                    if (!string.IsNullOrEmpty(siteRootRelativePath))
                    {
                        dirRelativePath += (siteRootRelativePath + "/");
                    }

                    var destinationUrl = publishProfile.GetWebDeployUrl();
                    TestEasyLog.Instance.Info(string.Format("Publishing directory '{0}' to website path '{1}'", directory, destinationUrl));

                    WebDeployHelper.DeployDirectory(directory, dirRelativePath, destinationUrl, publishProfile.UserName,
                        publishProfile.UserPassword, deleteExisting, paramResolverFunc);
                    break;
                }
                case PublishMethod.Ftp:
                {
                    var publishUrl = publishProfile.PublishUrl;
                    while (levelUpRequested > 0)
                    {
                        var index = publishUrl.LastIndexOf('/');
                        publishUrl = publishUrl.Substring(0, index);
                        levelUpRequested--;
                    }

                    result = FtpHelper.Authorize(publishProfile.UserName, publishProfile.UserPassword)
                        .UploadDir(directory, publishUrl, siteRootRelativePath);
                    break;
                }
                default:
                    throw new Exception(string.Format("Deployment method '{0}' is not supported.", publishMethod));
            }

            return result;
        }
    }
}