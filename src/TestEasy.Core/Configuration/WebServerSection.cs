using System.Configuration;

namespace TestEasy.Core.Configuration
{
    /// <summary>
    ///     Web server config section 
    /// </summary>
    public sealed class WebServerSection : ConfigurationSection
    {
        /// <summary>
        ///     Collection of registered web server types
        /// </summary>
        [ConfigurationProperty("", IsDefaultCollection = true)]
        public WebServerTypeCollection Types
        {
            get
            {
                return (WebServerTypeCollection)base[""];
            }
        }

        /// <summary>
        ///     Currently selected web server type to be used by tests
        /// </summary>
        [ConfigurationProperty("type", IsRequired = true)]
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
        ///     Timeout to let web server to warm up at startup
        /// </summary>
        [ConfigurationProperty("startupTimeout", IsRequired = false, DefaultValue = 5000)]
        public int StartupTimeout
        {
            get
            {
                return (int)base["startupTimeout"];
            }
            internal set
            {
                base["startupTimeout"] = value;
            }
        }
    }
}
