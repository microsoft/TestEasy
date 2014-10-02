using System;

namespace TestEasy.Azure
{
    /// <summary>
    ///     Entry point for all TestEasy.Azure helpers. Create an instance of the portal 
    ///     having a subscription and access all azure helpers through its memebers and properties.
    /// </summary>
    public class AzurePortal : IDisposable
    {
        /// <summary>
        /// This method should be used if clients are used directly but not through AzurePortal instance
        /// </summary>
        public static void Authorize(Subscription subscription)
        {
            Dependencies.Subscription = subscription;
        }

        /// <summary>
        ///     ctor
        /// </summary>
        /// <param name="subscription"></param>
        public AzurePortal(Subscription subscription)
        {
            Dependencies.Subscription = subscription;
        }

        private AzureCloudServiceManager _cloudServices;

        /// <summary>
        ///     Cloud services helper
        /// </summary>
        public AzureCloudServiceManager CloudServices
        {
            get 
            {
                return _cloudServices ??
                       (_cloudServices = new AzureCloudServiceManager());
            }
        }

        private AzureStorageServiceManager _storage;

        /// <summary>
        ///     Storage helper
        /// </summary>
        public AzureStorageServiceManager Storage
        {
            get
            {
                return _storage ??
                       (_storage = new AzureStorageServiceManager());
            }
        }

        private AzureVirtualMachineManager _virtualMachines;

        /// <summary>
        ///     Virtual machines helper
        /// </summary>
        public AzureVirtualMachineManager VirtualMachines
        {
            get
            {
                return _virtualMachines ??
                       (_virtualMachines = new AzureVirtualMachineManager());
            }
        }

        private AzureWebSiteManager _webSites;

        /// <summary>
        ///     Web sites helper
        /// </summary>
        public AzureWebSiteManager WebSites
        {
            get
            {
                return _webSites ??
                       (_webSites = new AzureWebSiteManager(Dependencies.Subscription.DefaultWebSpace));
            }
        }

        private AzureSqlManager _sql;

        /// <summary>
        ///     Azure SQL helper
        /// </summary>
        public AzureSqlManager Sql
        {
            get
            {
                return _sql ??
                       (_sql = new AzureSqlManager());
            }
        }

        private AzureAffinityGroupManager _affinityGroups;

        /// <summary>
        ///     Affinity groups helper
        /// </summary>
        public AzureAffinityGroupManager AffinityGroups
        {
            get
            {
                return _affinityGroups ??
                       (_affinityGroups = new AzureAffinityGroupManager());
            }
        }

        /// <summary>
        ///     Cleans all Azure objects create during test execution (not thread safe)
        /// </summary>
        public void CleanupResources()
        {
            Dependencies.TestResourcesCollector.CleanupResources();
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                CleanupResources();
            }
        }

        #endregion
    }
}
