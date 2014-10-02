using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.WindowsAzure;
using TestEasy.Core;
using TestEasy.Core.Configuration;

namespace TestEasy.Azure
{
    /// <summary>
    ///     Represents Azure subscription. Can be initialized from publishsettings file downloaded
    ///     from Azure portal or explicit binary certificate and subscription id
    /// </summary>
    public class Subscription
    {
        /// <summary>
        ///     Subscription id
        /// </summary>
        public string SubscriptionId { get; set; }
        
        /// <summary>
        ///     Endpoint URL for REST API
        /// </summary>
        public string CoreEndpointUrl { get; set; }

        /// <summary>
        ///     Database domain
        /// </summary>
        public string DatabaseDomain { get; set; }

        /// <summary>
        ///     Default location used by REST API objects when user did not provide location paramater
        /// </summary>
        public string DefaultLocation { get; set; }

        /// <summary>
        ///     Default web space used by REST API objects when user did not provide location paramater
        /// </summary>
        public string DefaultWebSpace { get; set; }

        /// <summary>
        ///     Default website domain name used by REST API objects when user did not provide location paramater
        /// </summary>
        public string DefaultWebSitesDomainName { get; set; }

        /// <summary>
        ///     Default storage name used by REST API objects when user did not provide location paramater
        /// </summary>
        public string DefaultStorageName { get; set; }

        /// <summary>
        ///     Credentials object
        /// </summary>
        public SubscriptionCloudCredentials Credentials
        {
            get;
            private set;
        }

        /// <summary>
        ///     ctor: uses default subscription alias from config files )default.config or testsuite.config)
        /// </summary>
        public Subscription()
            :this(TestEasyConfig.Instance.Azure.DefaultSubscription)
        {            
        }

        /// <summary>
        ///     ctor: explicitly initialize Subscription given subscription id and certificate object
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="certificate"></param>
        public Subscription(string subscriptionId, X509Certificate2 certificate)
        {
            SetDefaults();

            SubscriptionId = subscriptionId;
            Credentials = new CertificateCloudCredentials(SubscriptionId, certificate);
        }
        
        /// <summary>
        ///     ctor: initializes Subscription object using provided subscription alias
        /// </summary>
        /// <param name="subscriptionAlias"></param>
        public Subscription(string subscriptionAlias)
        {
            if (string.IsNullOrEmpty(subscriptionAlias))
            {
                throw new Exception(string.Format("subscriptionAlias can not be empty. If you used default constructor new Subscription(), then make sure that <azure defaultSubscription=value is not empty and contains valid subscription alias name (in default.config or testsuite.config files)."));
            }

            var subscription = TestEasyConfig.Instance.Azure.Subscriptions[subscriptionAlias];
            if (subscription == null)
            {
                throw new Exception(string.Format("Subscription '{0}' was not found in the configuration azure/subscriptions collection. Check default.config and testsuite.config and make sure that this subscription is registered in either of those configs.",
                    subscriptionAlias));    
            }

            var publishSettingsFile = subscription.PublishSettingsFile;

            if (!File.Exists(publishSettingsFile))
            {
                publishSettingsFile = Path.Combine(TestEasyConfig.Instance.Tools.DefaultRemoteToolsPath,
                    publishSettingsFile);
                if (!File.Exists(publishSettingsFile))
                {
                    throw new FileNotFoundException(
                        string.Format(
                            "Subscription publish settings file '{0}' was not found. Please specify correct path in default.config or testsuite.config under azure/subscriptions collection element corresponding to your subscription '{1}'.",
                            subscription.PublishSettingsFile, subscription.Name));
                }
            }

            Initialize(new FileInfo(publishSettingsFile));
        }

        /// <summary>
        ///     ctor: initialize subscription from *.publishsettings file
        /// </summary>
        /// <param name="publishSettingsFile"></param>
        public Subscription(FileInfo publishSettingsFile)
        {
            Initialize(publishSettingsFile);
        }

        private void Initialize(FileInfo publishSettingsFile)
        {
            SetDefaults();

            string certString;
            using (var fs = AbstractionsLocator.Instance.FileSystem.FileOpenRead(publishSettingsFile.FullName))
            {
                var document = XDocument.Load(fs);

                var subscriptionNode = document.XPathSelectElements("/PublishData/PublishProfile/Subscription").First();

                SubscriptionId = subscriptionNode.Attribute("Id").Value;
                certString = subscriptionNode.Attribute("ManagementCertificate").Value;
                CoreEndpointUrl = subscriptionNode.Attribute("ServiceManagementUrl").Value;
            }

            Credentials = new CertificateCloudCredentials(SubscriptionId, new X509Certificate2(Convert.FromBase64String(certString)));            
        }

        private void SetDefaults()
        {
            CoreEndpointUrl = AzureServiceConstants.AzureCoreEndPointUrl;
            DefaultLocation = AzureServiceConstants.DefaultLocation;
            DefaultWebSpace = AzureServiceConstants.DefaultWebSpace;
            DefaultWebSitesDomainName = AzureServiceConstants.AzureWebSitesDomainName;
            DatabaseDomain = AzureServiceConstants.AzureSqlDefaultDomain;
            DefaultStorageName = AzureServiceConstants.DefaultStorageServiceName;
        }
    }
}
