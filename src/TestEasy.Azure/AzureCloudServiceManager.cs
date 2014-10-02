using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Compute.Models;
using Microsoft.WindowsAzure.Management.Storage.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TestEasy.Azure.Helpers;
using TestEasy.Core;
using TestEasy.Core.Abstractions;
using TestEasy.Core.Helpers;

namespace TestEasy.Azure
{
    /// <summary>
    ///     Contains helper methods for managing cloud services
    /// </summary>
    public class AzureCloudServiceManager
    {
        private readonly IWebRequestor _webRequestor;

        /// <summary>
        ///     Rest API client
        /// </summary>
        public IComputeManagementClient ComputeManagementClient
        {
            get;
            private set;
        }

        internal AzureCloudServiceManager()
            : this(new WebRequestor())
        {
        }

        internal AzureCloudServiceManager(IWebRequestor requestor)
        {
            _webRequestor = requestor;
            ComputeManagementClient = new ComputeManagementClient(Dependencies.Subscription.Credentials);
        }

        /// <summary>
        ///     Create hosted service
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <returns></returns>
        public string CreateHostedService(string hostedServiceName = "")
        {
            if (string.IsNullOrEmpty(hostedServiceName))
            {
                hostedServiceName = Dependencies.TestResourcesCollector.GetUniqueHostedServiceName();
            }

            const string affinityGroup = AzureServiceConstants.DefaultAffinityGroup;

            return CreateHostedService(new HostedServiceCreateParameters
                {
                    ServiceName = hostedServiceName,
                    Label = Base64EncodingHelper.EncodeToBase64String(AzureServiceConstants.DefaultLabel),
                    Description = "",
                    AffinityGroup = affinityGroup
                });
        }

        /// <summary>
        ///     Create hosted service
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string CreateHostedService(HostedServiceCreateParameters input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (HostedServiceExists(input.ServiceName)) return input.ServiceName;

            var affinityGroupManager = new AzureAffinityGroupManager();
            affinityGroupManager.EnsureAffinityGroupExists(input.AffinityGroup);

            ComputeManagementClient.HostedServices.CreateAsync(input, new CancellationToken()).Wait();

            Dependencies.TestResourcesCollector.Remember(AzureResourceType.HostedService, input.ServiceName, input.ServiceName);

            return input.ServiceName;
        }

        /// <summary>
        ///     Return hosted service by name
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <returns></returns>
        public HostedServiceGetResponse GetHostedService(string hostedServiceName)
        {
            HostedServiceGetResponse result;
            try
            {
                result = ComputeManagementClient.HostedServices.GetAsync(hostedServiceName, new CancellationToken()).Result;
            }
            catch
            {
                return null;
            }

            return result;
        }

        /// <summary>
        ///     Get storage accounts that are used by hosted service
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <returns></returns>
        public IList<StorageAccount> GetStorageAccountsUsedByHostedService(string hostedServiceName)
        {
            var uniqueStorageAccounts = new HashSet<StorageAccount>();
            var storageManager = new AzureStorageServiceManager();
            var vhdManager = new AzureVirtualMachineManager();
            var vhdsLeased = vhdManager.GetVirtualHardDisks().Where(vhd => vhd.UsageDetails.HostedServiceName == hostedServiceName);
            foreach (var vhd in vhdsLeased)
            {
                var storageAccount = storageManager.GetStorageServiceForBlob(vhd.MediaLinkUri);
                uniqueStorageAccounts.Add(storageAccount);
            }

            return uniqueStorageAccounts.ToList();
        }

        /// <summary>
        ///     Checks if hosted service exists
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <returns></returns>
        public bool HostedServiceExists(string hostedServiceName)
        {
            return GetHostedService(hostedServiceName) != null;
        }

        /// <summary>
        ///     Delete hosted service
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="breakVhdLeases"></param>
        public void DeleteHostedService(string hostedServiceName, bool breakVhdLeases = false)
        {
            ComputeManagementClient.HostedServices.DeleteAsync(hostedServiceName, new CancellationToken()).Wait();
            Dependencies.TestResourcesCollector.Forget(AzureResourceType.HostedService, hostedServiceName);

            if (breakVhdLeases)
            {
                BreakVhdLeases(hostedServiceName);
            }
        }

        /// <summary>
        ///     Break leases on vhd blobs
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="vhdBlobContainer"></param>
        public void BreakVhdLeases(string hostedServiceName, string vhdBlobContainer = "vhds")
        {
            var storageManager = new AzureStorageServiceManager();
            var vhdManager = new AzureVirtualMachineManager();
            var vhdsLeased = vhdManager.GetVirtualHardDisks().Where(vhd => vhd.UsageDetails.HostedServiceName == hostedServiceName);
            foreach (var vhd in vhdsLeased)
            {
                var storage = storageManager.GetStorageServiceForBlob(vhd.MediaLinkUri);
                var blob = storage.GetBlob(Path.GetFileName(vhd.MediaLinkUri.ToString()), vhdBlobContainer);

                TestEasyLog.Instance.Info("Breaking lease on [" + blob.Uri + "]");

                blob.BreakLease(TimeSpan.FromSeconds(1));
            }

            TestEasyLog.Instance.Info("Waiting for VHDs to be released");
            // now wait a short while for the lease to actually expire.
            RetryHelper.RetryUntil(() =>
            {
                var remainingVhdsLeased = vhdManager.GetVirtualHardDisks().Where(vhd => vhd.UsageDetails.HostedServiceName == hostedServiceName);
                return !remainingVhdsLeased.Any();
            }, 30, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        ///     Return list of hosted services in subscription
        /// </summary>
        /// <returns></returns>
        public IList<HostedServiceListResponse.HostedService> GetHostedServices()
        {
            var rawList = ComputeManagementClient.HostedServices.ListAsync(new CancellationToken()).Result;

            return rawList.HostedServices;
        }

        /// <summary>
        ///     Update hosted service
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="input"></param>
        public void UpdateHostedService(string hostedServiceName, HostedServiceUpdateParameters input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (string.IsNullOrEmpty(hostedServiceName))
            {
                throw new Exception("Hosted service name is null or empty.");
            }

            if (!HostedServiceExists(hostedServiceName))
            {
                throw new Exception(string.Format("No hosted service exist with name '{0}'", hostedServiceName));
            }

            ComputeManagementClient.HostedServices.UpdateAsync(hostedServiceName, input, new CancellationToken()).Wait();
        }

        /// <summary>
        ///     Create or update hosted service deployment
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="packageLocation"></param>
        /// <param name="configPath"></param>
        /// <param name="deploymentName"></param>
        /// <param name="deploymentSlot"></param>
        /// <returns></returns>
        public DeploymentGetResponse CreateOrUpdateDeployment(string hostedServiceName, Uri packageLocation, string configPath, string deploymentName = "",
            DeploymentSlot deploymentSlot = DeploymentSlot.Production)
        {
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException(string.Format("The file '{0}' cannot be found or the caller does not have sufficient permissions to read it.", configPath));
            }

            if (string.IsNullOrEmpty(deploymentName))
            {
                deploymentName = Dependencies.TestResourcesCollector.GetUniqueDeploymentName();
            }

            return CreateOrUpdateDeployment(hostedServiceName,
                new DeploymentCreateParameters
                {
                    Name = deploymentName,
                    Configuration = GetSettingsFromPackageConfig(configPath),
                    PackageUri = packageLocation,
                    Label = AzureServiceConstants.DefaultLabel,
                    StartDeployment = true,
                    TreatWarningsAsError = false
                },
                deploymentSlot);
        }

        /// <summary>
        ///     Create or update hosted service deployment
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="input"></param>
        /// <param name="deploymentSlot"></param>
        /// <returns></returns>
        public DeploymentGetResponse CreateOrUpdateDeployment(string hostedServiceName,
                                               DeploymentCreateParameters input,
                                               DeploymentSlot deploymentSlot = DeploymentSlot.Production)
        {
            return GetDeployment(hostedServiceName, input.Name) ?? CreateDeployment(hostedServiceName, input, deploymentSlot);
        }

        /// <summary>
        ///     Create hosted service deployment
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="input"></param>
        /// <param name="deploymentSlot"></param>
        /// <returns></returns>
        public DeploymentGetResponse CreateDeployment(string hostedServiceName, DeploymentCreateParameters input,
            DeploymentSlot deploymentSlot = DeploymentSlot.Production)
        {
            TestEasyLog.Instance.Info(string.Format("Creating Deployment... Name: '{0}', Label: '{1}'", input.Name, input.Label));

            ComputeManagementClient.Deployments.CreateAsync(hostedServiceName,
                deploymentSlot,
                input,
                new CancellationToken()).Wait();

            var result = GetDeployment(hostedServiceName, input.Name);

            Dependencies.TestResourcesCollector.Remember(
                AzureResourceType.Deployment,
                result.Name,
                new DeploymentInfo { Deployment = result, HostedService = hostedServiceName });

            return result;
        }

        /// <summary>
        ///     Returns hosted service deployment
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="deploymentName"></param>
        /// <returns></returns>
        public DeploymentGetResponse GetDeployment(string hostedServiceName, string deploymentName)
        {
            try
            {
                var result = ComputeManagementClient.Deployments.GetByNameAsync(hostedServiceName, deploymentName, new CancellationToken()).Result;

                return result;
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        ///     Returns hosing service deployment by slot
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        public DeploymentGetResponse GetDeploymentBySlot(string hostedServiceName, DeploymentSlot slot)
        {
            try
            {
                var result = ComputeManagementClient.Deployments.GetBySlotAsync(hostedServiceName, slot, new CancellationToken()).Result;

                return result;
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        ///     Update hosted service deployment status
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="deploymentName"></param>
        /// <param name="status"></param>
        public void UpdateDeploymentStatus(string hostedServiceName, string deploymentName, UpdatedDeploymentStatus status)
        {
            ComputeManagementClient.Deployments.UpdateStatusByDeploymentNameAsync(hostedServiceName, deploymentName, new DeploymentUpdateStatusParameters
            {
                Status = status,
            }, new CancellationToken()).Wait();
        }

        /// <summary>
        ///     Update hosted service deployment by slot
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="slot"></param>
        /// <param name="status"></param>
        public void UpdateDeploymentStatusBySlot(string hostedServiceName, DeploymentSlot slot, UpdatedDeploymentStatus status)
        {
            ComputeManagementClient.Deployments.UpdateStatusByDeploymentSlotAsync(hostedServiceName,
                slot,
                new DeploymentUpdateStatusParameters { Status = status },
                new CancellationToken()).Wait();
        }

        /// <summary>
        ///     Delete hosted service deployment
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="deploymentName"></param>
        public void DeleteDeployment(string hostedServiceName, string deploymentName)
        {
            ComputeManagementClient.Deployments.DeleteByNameAsync(hostedServiceName, deploymentName, true, new CancellationToken()).Wait();
            Dependencies.TestResourcesCollector.Forget(AzureResourceType.Deployment, deploymentName);
        }

        /// <summary>
        ///     Delete hosted service deployment by slot
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="slot"></param>
        public void DeleteDeploymentBySlot(string hostedServiceName, DeploymentSlot slot)
        {
            ComputeManagementClient.Deployments.DeleteBySlotAsync(hostedServiceName, slot, new CancellationToken()).Wait();
        }

        /// <summary>
        ///     Swap deployment from/to production or testing
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="sourceDeploymetName"></param>
        /// <param name="productionDeploymentName"></param>
        public void SwapDeployment(string hostedServiceName, string sourceDeploymetName, string productionDeploymentName)
        {
            SwapDeployment(hostedServiceName, new DeploymentSwapParameters
                {
                    SourceDeployment = sourceDeploymetName,
                    ProductionDeployment = productionDeploymentName
                });
        }

        /// <summary>
        ///     Swap deployment from/to production or testing
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="input"></param>
        public void SwapDeployment(string hostedServiceName, DeploymentSwapParameters input)
        {
            ComputeManagementClient.Deployments.SwapAsync(hostedServiceName, input, new CancellationToken()).Wait();
        }

        /// <summary>
        ///     Deploy worker role
        /// </summary>
        /// <param name="packagePath"></param>
        /// <param name="configPath"></param>
        /// <param name="roleName"></param>
        /// <param name="deploymentSlot"></param>
        /// <returns></returns>
        public DeploymentInfo DeployRole(string packagePath, string configPath, string roleName = "",
            DeploymentSlot deploymentSlot = DeploymentSlot.Production)
        {
            var storageManager = new AzureStorageServiceManager();

            var service = storageManager.CreateStorageService();
            var blob = service.CreateBlob(Path.GetFileName(packagePath));
            var packageBlobUri = blob.UploadFile(packagePath);

            roleName = CreateHostedService(roleName);

            var deployment = CreateOrUpdateDeployment(roleName, packageBlobUri, configPath, deploymentSlot: deploymentSlot);

            return new DeploymentInfo { HostedService = roleName, Deployment = deployment };
        }

        /// <summary>
        ///     Delete worker role
        /// </summary>
        /// <param name="role"></param>
        public void DeleteRole(DeploymentInfo role)
        {
            // blob and container will be deleted later when portal is disposed
            DeleteDeployment(role.HostedService, role.Deployment.Name);
        }

        /// <summary>
        ///     Reboote worker role
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="deploymentName"></param>
        /// <param name="roleInstanceName"></param>
        public void RebootDeploymentRoleInstance(string hostedServiceName, string deploymentName, string roleInstanceName)
        {
            ComputeManagementClient.Deployments.RebootRoleInstanceByDeploymentNameAsync(hostedServiceName, deploymentName, roleInstanceName, new CancellationToken()).Wait();
        }

        /// <summary>
        ///     Reboot worker role
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="slot"></param>
        /// <param name="roleInstanceName"></param>
        public void RebootDeploymentRoleInstanceBySlot(string hostedServiceName, DeploymentSlot slot, string roleInstanceName)
        {
            ComputeManagementClient.Deployments.RebootRoleInstanceByDeploymentSlotAsync(
                hostedServiceName,
                slot,
                roleInstanceName,
                new CancellationToken());
        }

        /// <summary>
        ///     starte worker role
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="deploymentName"></param>
        public void StartRole(string hostedServiceName, string deploymentName)
        {
            UpdateDeploymentStatus(hostedServiceName, deploymentName, UpdatedDeploymentStatus.Running);
        }

        /// <summary>
        ///     Stop worker role
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="deploymentName"></param>
        public void StopRole(string hostedServiceName, string deploymentName)
        {
            UpdateDeploymentStatus(hostedServiceName, deploymentName, UpdatedDeploymentStatus.Suspended);
        }

        /// <summary>
        ///     Wait for role to respond
        /// </summary>
        /// <param name="role"></param>
        /// <param name="timeoutInMilliSeconds"></param>
        /// <returns></returns>
        public bool WaitForRoleToRespond(DeploymentInfo role, int timeoutInMilliSeconds = 900000)
        {
            return WaitForRoleToRespond(role.Deployment.Uri.ToString(), timeoutInMilliSeconds);
        }

        public bool WaitForRoleToRespond(string url, int timeoutInMilliSeconds = 900000)
        {
            return RetryHelper.RetryUntil(() => _webRequestor.PingUrl(url), timeoutInMilliSeconds / 1000, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        ///     Get settings string from package config
        /// </summary>
        /// <param name="settingsFile"></param>
        /// <returns></returns>
        private static string GetSettingsFromPackageConfig(string settingsFile)
        {
            string settings;

            try
            {
                settings = String.Join("", File.ReadAllLines(Path.GetFullPath(settingsFile)));
            }
            catch (Exception e)
            {
                TestEasyLog.Instance.Info(string.Format("Error reading settings from file '{0}': '{1}' ", settingsFile, e.Message));
                throw;
            }
            return settings;
        }
    }
}