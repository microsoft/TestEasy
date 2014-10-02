using TestEasy.Core.Helpers;

namespace TestEasy.Core
{
    /// <summary>
    ///     Centralized container for all helpers to be used in TestEasy assemblies and in tests
    /// </summary>
    public class TestEasyHelpers
    {
        private static XmlHelper _xmlHelper;
        public static XmlHelper Xml
        {
            get { return _xmlHelper ?? (_xmlHelper = new XmlHelper()); }
            internal set { _xmlHelper = value; }
        }

        private static FirewallHelper _firewallHelper;
        public static FirewallHelper Firewall
        {
            get { return _firewallHelper ?? (_firewallHelper = new FirewallHelper()); }
            internal set { _firewallHelper = value; }
        }

        private static ToolsHelper _toolsHelper;
        public static ToolsHelper Tools
        {
            get { return _toolsHelper ?? (_toolsHelper = new ToolsHelper()); }
            internal set { _toolsHelper = value; }
        }

        private static WebHelper _webHelper;
        public static WebHelper Web
        {
            get { return _webHelper ?? (_webHelper = new WebHelper()); }
            internal set { _webHelper = value; }
        }

        private static BuildHelper _buildHelper;
        public static BuildHelper Builder
        {
            get { return _buildHelper ?? (_buildHelper = new BuildHelper()); }
            internal set { _buildHelper = value; }
        }
    }
}
