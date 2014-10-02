namespace TestEasy.WebServer
{
    /// <summary>
    ///     Type of the object to be deployed to web server
    /// </summary>
    public enum DeploymentItemType
    {
        Directory,
        File,
        Content
    }

    /// <summary>
    ///     Object to be deployed to web server
    /// </summary>
    public class DeploymentItem
    {
        /// <summary>
        ///     object type
        /// </summary>
        public DeploymentItemType Type { get; set; }

        /// <summary>
        ///     Path to the source to be deployed (file or folder)
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        ///     Content of the file to be deployed
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        ///     Relative path on server where object should be deployed
        /// </summary>
        public string TargetRelativePath { get; set; } // relative to web app root
    }
}
