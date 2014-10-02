using System.Configuration;

namespace TestEasy.Core.Configuration
{
    /// <summary>
    ///     Config section for tools settings
    /// </summary>
    public sealed class ToolsSection : ConfigurationSection
    {
        /// <summary>
        ///     Collection of known tools
        /// </summary>
        [ConfigurationProperty("", IsDefaultCollection = true)]
        public ToolsCollection Tools
        {
            get
            {
                return (ToolsCollection)base[""];
            }
        }

        /// <summary>
        ///     remote path where to download tools from
        /// </summary>
        [ConfigurationProperty("defaultRemoteToolsPath", IsRequired = false, DefaultValue = "[YOUR_REMOTE_SHARE_PATH]")]
        public string DefaultRemoteToolsPath
        {
            get
            {
                return (string)base["defaultRemoteToolsPath"];
            }
            internal set
            {
                base["defaultRemoteToolsPath"] = value;
            }
        }

        /// <summary>
        ///     Path on local machine where to download tools to
        /// </summary>
        [ConfigurationProperty("defaultLocalToolsPath", IsRequired = false, DefaultValue = TestEasyConfig.DefaultLocalToolsPath)]
        public string DefaultLocalToolsPath
        {
            get
            {
                return (string)base["defaultLocalToolsPath"];
            }
            internal set
            {
                base["defaultLocalToolsPath"] = value;
            }
        }
    }
}
