using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Compute.Models;
using TestEasy.Azure.Helpers;
using TestEasy.Core;

namespace TestEasy.Azure
{
    /// <summary>
    ///     Contains helper methods for managing Azure Machines
    /// </summary>
    public class AzureVirtualMachineManager
    {
        /// <summary>
        ///     Rest API client
        /// </summary>
        public IComputeManagementClient ComputeManagementClient
        {
            get;
            private set;
        }

        internal AzureVirtualMachineManager()
        {
            ComputeManagementClient = new ComputeManagementClient(Dependencies.Subscription.Credentials,
                new Uri(Dependencies.Subscription.CoreEndpointUrl));
        }

        /// <summary>
        ///     Create virtual machine
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="osVirtualHardDisk"></param>
        /// <param name="configurationSets"></param>
        /// <param name="dataVirtualHardDisks"></param>
        /// <param name="virtualMachineName"></param>
        /// <param name="deploymentName"></param>
        /// <returns></returns>
        public string CreateVirtualMachine(
            string hostedServiceName,
            OSVirtualHardDisk osVirtualHardDisk,
            IList<ConfigurationSet> configurationSets,
            IList<DataVirtualHardDisk> dataVirtualHardDisks = null,
            string virtualMachineName = "",
            string deploymentName = "")
        {
            if (string.IsNullOrEmpty(deploymentName))
            {
                deploymentName = Dependencies.TestResourcesCollector.GetUniqueDeploymentName();
            }

            if (string.IsNullOrEmpty(virtualMachineName))
            {
                virtualMachineName = Dependencies.TestResourcesCollector.GetUniqueVirtualMachineName();
            }

            var csm = new AzureCloudServiceManager();
            csm.CreateOrUpdateDeployment(hostedServiceName, new DeploymentCreateParameters { Name = deploymentName });

            var vmDeployment = csm.CreateOrUpdateDeployment(hostedServiceName, new DeploymentCreateParameters { 
                Name = deploymentName, 
                Label = Base64EncodingHelper.EncodeToBase64String(AzureServiceConstants.DefaultLabel)
            });
                
            var vmRole = new Role
            {
                RoleName = virtualMachineName,
                RoleType = "PersistentVMRole",
                OSVirtualHardDisk = osVirtualHardDisk,
                ConfigurationSets = configurationSets,
                DataVirtualHardDisks = dataVirtualHardDisks
            };

            vmDeployment.Roles.Add(vmRole);

            return CreateVirtualMachine(hostedServiceName, vmDeployment, virtualMachineName);
        }

        /// <summary>
        ///     Create virtual machine
        /// </summary>
        /// <param name="hostedServiceName"></param>
        /// <param name="input"></param>
        /// <param name="virtualMachineName"></param>
        /// <returns></returns>
        public string CreateVirtualMachine(string hostedServiceName, DeploymentGetResponse input, string virtualMachineName)
        {
            var role = input.Roles.First();

            TestEasyLog.Instance.Info(string.Format("Creating Virtual Machine... DeploymentName: {0}, Name: {1}",
                                        input.Name, virtualMachineName));

            ComputeManagementClient.VirtualMachines.CreateAsync(hostedServiceName, input.Name, 
                ConvertRoleToVirtualMachineCreateParameters(role), new CancellationToken()).Wait();

            return virtualMachineName;
        }

        /// <summary>
        ///     Get list of available OS images
        /// </summary>
        /// <returns></returns>
        public IList<VirtualMachineOSImageListResponse.VirtualMachineOSImage> GetOsImages()
        {
            TestEasyLog.Instance.Info("Listing OS Images...");

            var result = ComputeManagementClient.VirtualMachineOSImages.ListAsync(new CancellationToken()).Result;

            TestEasyLog.Instance.LogObject(result.Images);

            return result.Images;
        }

        /// <summary>
        ///     Get list of available virtual hard drives
        /// </summary>
        /// <returns></returns>
        public IList<VirtualMachineDiskListResponse.VirtualMachineDisk> GetVirtualHardDisks()
        {
            TestEasyLog.Instance.Info("Getting Virtual Hard Disks...");

            var result = ComputeManagementClient.VirtualMachineDisks.ListDisksAsync(new CancellationToken()).Result;

            TestEasyLog.Instance.LogObject(result.Disks);

            return result.Disks;
        }

        private static VirtualMachineCreateParameters ConvertRoleToVirtualMachineCreateParameters(Role role)
        {
            return new VirtualMachineCreateParameters
            {
                AvailabilitySetName = role.AvailabilitySetName,
                ConfigurationSets = role.ConfigurationSets,
                DataVirtualHardDisks = role.DataVirtualHardDisks,
                OSVirtualHardDisk = role.OSVirtualHardDisk,
                ProvisionGuestAgent = role.ProvisionGuestAgent,
                ResourceExtensionReferences = role.ResourceExtensionReferences,
                RoleName = role.RoleName,
                RoleSize = role.RoleSize,
                VMImageName = role.VMImageName,
            };
        }
    }
}