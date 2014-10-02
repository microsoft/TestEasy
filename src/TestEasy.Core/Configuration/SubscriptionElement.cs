using System.Configuration;

namespace TestEasy.Core.Configuration
{
    /// <summary>
    ///     Subscription config element
    /// </summary>
    public sealed class SubscriptionElement : ConfigurationElement
    {
        /// <summary>
        ///     Subscription internal ("alias") name
        /// </summary>
        [ConfigurationProperty("name", IsKey = true)]
        public string Name
        {
            get
            {
                return (string)(base["name"]);
            }
            internal set
            {
                base["name"] = value;
            }
        }

        /// <summary>
        ///     publishSettingsFile path or just a file name
        /// </summary>
        [ConfigurationProperty("publishSettingsFile", IsKey = false, DefaultValue = "")]
        public string PublishSettingsFile
        {
            get
            {
                return (string)(base["publishSettingsFile"]);
            }
            internal set
            {
                base["publishSettingsFile"] = value;
            }
        }
    }
}
