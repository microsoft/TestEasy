using System.Configuration;

namespace TestEasy.Core.Configuration
{
    /// <summary>
    ///     Config element for web server type element
    /// </summary>
    public sealed class WebServerTypeElement : ConfigurationElement
    {
        /// <summary>
        ///     Web server name
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
        ///     .Net type for web server wrapper
        /// </summary>
        [ConfigurationProperty("type", IsKey = false, DefaultValue = "")]
        public string Type
        {
            get
            {
                return (string)(base["type"]);
            }
            internal set
            {
                base["type"] = value;
            }
        }
    }
}
