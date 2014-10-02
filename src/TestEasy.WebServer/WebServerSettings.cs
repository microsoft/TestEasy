namespace TestEasy.WebServer
{
    /// <summary>
    ///     Settings that passed to WebServer constructor for initialization
    /// </summary>
    public class WebServerSettings
    {
        /// <summary>
        ///     Time interval to let web server process to warm up after start before serving requests (milliseconds)
        /// </summary>
        public int StartupTimeout { get; set; }

        /// <summary>
        ///     Architecture of the web server that was requested by test
        /// </summary>
        public Architecture Architecture { get; set; }

        /// <summary>
        ///     Physical path to the rootfolder for web server
        /// </summary>
        public string RootPhysicalPath { get; set; }

        /// <summary>
        ///     Host name that web server is using in virtual paths to web applications
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        ///     Name of the web application that should be added to wbe server as soon at it is started
        /// </summary>
        public string WebAppName { get; set; }
        
        /// <summary>
        ///     ctor
        /// </summary>
        public WebServerSettings()
        {
            StartupTimeout = 5000;
            Architecture = Architecture.Default;
            RootPhysicalPath = "";
            HostName = "localhost";
            WebAppName = "";
        }
    }
}
