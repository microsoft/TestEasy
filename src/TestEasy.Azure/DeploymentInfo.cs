using Microsoft.WindowsAzure.Management.Compute.Models;

namespace TestEasy.Azure
{
    /// <summary>
    ///     Azure deployment information model
    /// </summary>
    public class DeploymentInfo
    {
        /// <summary>
        ///     Hosted service name
        /// </summary>
        public string HostedService { get; set; }

        /// <summary>
        ///     Deployment details
        /// </summary>
        public DeploymentGetResponse Deployment { get; set; }
    }

}
