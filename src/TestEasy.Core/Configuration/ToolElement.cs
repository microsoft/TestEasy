using System.Configuration;

namespace TestEasy.Core.Configuration
{
    /// <summary>
    ///     Tools config element
    /// </summary>
    public sealed class ToolElement : ConfigurationElement
    {
        /// <summary>
        ///     Tool's internal name
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
        ///     Tool's real name
        /// </summary>
        [ConfigurationProperty("path", IsKey = false, DefaultValue = "")]
        public string Path
        {
            get
            {
                return (string)(base["path"]);
            }
            internal set
            {
                base["path"] = value;
            }
        }

        /// <summary>
        ///     Arguments to be passed whn tool executes
        /// </summary>
        [ConfigurationProperty("arguments", IsKey = false, DefaultValue = "")]
        public string Arguments
        {
            get
            {
                return (string)(base["arguments"]);
            }
            internal set
            {
                base["arguments"] = value;
            }
        }
    }
}
