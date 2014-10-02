using System.Configuration;

namespace TestEasy.Core.Configuration
{
    /// <summary>
    ///     Config section for client (browsers) settings
    /// </summary>
    public sealed class ClientSection : ConfigurationSection
    {
        /// <summary>
        ///     Currently chosen browser type to be used by tests
        /// </summary>
        [ConfigurationProperty("type", IsRequired = false, DefaultValue="Ie")]
        public string Type
        {
            get
            {
                return (string)base["type"];
            }
            internal set
            {
                base["type"] = value;
            }
        }

        /// <summary>
        ///     Are browsers execute remote requests to test web sites?
        /// </summary>
        [ConfigurationProperty("remote", IsRequired = false, DefaultValue = false)]
        public bool Remote
        {
            get
            {
                return (bool)base["remote"];
            }
            internal set
            {
                base["remote"] = value;
            }
        }

        /// <summary>
        ///     Remote selenium hub URL to connect to remote browsers
        /// </summary>
        [ConfigurationProperty("remoteHubUrl", IsRequired = false, DefaultValue = "")]
        public string RemoteHubUrl
        {
            get
            {
                return (string)base["remoteHubUrl"];
            }
            internal set
            {
                base["remoteHubUrl"] = value;
            }
        }
    }
}
