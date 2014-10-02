using System.Configuration;

namespace TestEasy.Core.Configuration
{
    /// <summary>
    ///     Config section for azure settings
    /// </summary>
    public sealed class AzureSection : ConfigurationSection
    {
        /// <summary>
        ///     Collection of known subscriptions
        /// </summary>
        [ConfigurationProperty("", IsDefaultCollection = true)]
        public SubscriptionsCollection Subscriptions
        {
            get
            {
                return (SubscriptionsCollection)base[""];
            }
        }

        /// <summary>
        ///     default subscription that will be used by AzurePortal ctor without parameters
        /// </summary>
        [ConfigurationProperty("defaultSubscription", IsRequired = false, DefaultValue = "")]
        public string DefaultSubscription
        {
            get
            {
                return (string)base["defaultSubscription"];
            }
            internal set
            {
                base["defaultSubscription"] = value;
            }
        }
    }
}
