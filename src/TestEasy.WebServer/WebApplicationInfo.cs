namespace TestEasy.WebServer
{
    /// <summary>
    ///     Web application's properties
    /// </summary>
    public class WebApplicationInfo
    {
        /// <summary>
        ///     Web application name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Web application physical path
        /// </summary>
        public string PhysicalPath { get; set; }

        /// <summary>
        ///     Web appication virtual path (depends on remote=true/false 
        ///     in config and is preferrable to be used in tests)
        /// </summary>
        public string VirtualPath { get; set; }

        /// <summary>
        ///     Web application remote virtual path 
        /// </summary>
        public string RemoteVirtualPath { get; set; }
    }
}
