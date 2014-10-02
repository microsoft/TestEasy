using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.WindowsAzure.Management;
using Microsoft.WindowsAzure.Management.Models;
using TestEasy.Core;

namespace TestEasy.Azure
{
    /// <summary>
    ///     Contains helper methods for managing Azure Machines
    /// </summary>
    public class AzureAffinityGroupManager
    {
        /// <summary>
        ///     Rest API client
        /// </summary>
        public IManagementClient ManagementClient
        {
            get;
            private set;
        }

        internal AzureAffinityGroupManager()
        {
            ManagementClient = new ManagementClient(Dependencies.Subscription.Credentials, new Uri(Dependencies.Subscription.CoreEndpointUrl));
        }

        /// <summary>
        ///     Create affinity group
        /// </summary>
        /// <param name="name"></param>
        public void CreateAffinityGroup(string name)
        {
            CreateAffinityGroup(new AffinityGroupCreateParameters
                {
                    Name = name,
                    Location = AzureServiceConstants.DefaultLocation,
                    Label = AzureServiceConstants.DefaultLabel
                });
        }

        /// <summary>
        ///     Create affinity group
        /// </summary>
        /// <param name="input"></param>
        public void CreateAffinityGroup(AffinityGroupCreateParameters input)
        {
            TestEasyLog.Instance.Info(string.Format("Creating affinity group '{0}'", input.Name));

            ManagementClient.AffinityGroups.CreateAsync(input, new CancellationToken()).Wait();

            Dependencies.TestResourcesCollector.Remember(AzureResourceType.AffinityGroup, input.Name, input.Name);

        }

        /// <summary>
        ///     Update affinity group
        /// </summary>
        /// <param name="name"></param>
        public void UpdateAffinityGroup(string name)
        {
            UpdateAffinityGroup(name, new AffinityGroupUpdateParameters
            {
                Label = AzureServiceConstants.DefaultLabel
            });
        }

        /// <summary>
        ///     Update affinity group
        /// </summary>
        /// <param name="name"></param>
        /// <param name="input"></param>
        public void UpdateAffinityGroup(string name, AffinityGroupUpdateParameters input)
        {
            TestEasyLog.Instance.Info(string.Format("Updating affinity group '{0}'", name));
            ManagementClient.AffinityGroups.UpdateAsync(name, input, new CancellationToken()).Wait();
        }

        /// <summary>
        ///     Delete affinity group
        /// </summary>
        /// <param name="name"></param>
        public void DeleteAffinityGroup(string name)
        {
            TestEasyLog.Instance.Info(string.Format("Deleting affinity group '{0}'", name));
            ManagementClient.AffinityGroups.DeleteAsync(name, new CancellationToken()).Wait();
            Dependencies.TestResourcesCollector.Forget(AzureResourceType.AffinityGroup, name);
        }

        /// <summary>
        ///     Get affinity group
        /// </summary>
        /// <returns></returns>
        public IList<AffinityGroupListResponse.AffinityGroup> GetAffinityGroups()
        {
            TestEasyLog.Instance.Info("Getting all affinity groups");
            var allAffinityGroups = ManagementClient.AffinityGroups.ListAsync(new CancellationToken()).Result;

            return allAffinityGroups.AffinityGroups;
        }

        /// <summary>
        ///     Get affinity group
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public AffinityGroupListResponse.AffinityGroup GetAffinityGroup(string name)
        {
            TestEasyLog.Instance.Info(string.Format("Getting affinity group '{0}'", name));
            try
            {
                var result = ManagementClient.AffinityGroups.GetAsync(name, new CancellationToken()).Result;

                var affinityGroup = new AffinityGroupListResponse.AffinityGroup
                {
                    Capabilities = result.Capabilities,
                    Description = result.Description,
                    Label = result.Label,
                    Location = result.Location,
                    Name = result.Name,
                };

                TestEasyLog.Instance.LogObject(affinityGroup);

                return affinityGroup;
            }
            catch (Exception)
            {
                TestEasyLog.Instance.Warning(string.Format("Affinity group '{0}' not found", name));
            }

            return null;
        }

        /// <summary>
        ///     Get unique affinity group
        /// </summary>
        /// <returns></returns>
        public string GetUniqueAffinityGroup()
        {
            return Dependencies.TestResourcesCollector.GetUniqueAffinityGroup();
        }

        /// <summary>
        ///     Ensure affinity group exist, if it is not - create it
        /// </summary>
        /// <param name="name"></param>
        public void EnsureAffinityGroupExists(string name)
        {
            var groups = GetAffinityGroups();
            if (groups.All(g => string.Compare(g.Name, name, StringComparison.OrdinalIgnoreCase) != 0))
            {
                CreateAffinityGroup(new AffinityGroupCreateParameters
                {
                    Name = name,
                    Location = Dependencies.Subscription.DefaultLocation,
                    Label = AzureServiceConstants.DefaultLabel
                });
            }
        }

    }
}