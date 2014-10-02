using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Blob;
using TestEasy.Azure.Sql;
using TestEasy.Core;
using WAML = Microsoft.WindowsAzure.Management;

namespace TestEasy.Azure
{
    internal enum AzureResourceType
    {
        AffinityGroup,
        StorageService,
        HostedService,
        StorageBlob,
        StorageContainer,
        Deployment,
        WebSite,
        SqlServer,
        SqlDatabase
    }

    internal delegate void AzureResourceCleaner(AzureResourceType type, Dictionary<string, object> resources);

    /// <summary>
    ///     Respnsible for storing information about used Azure resources and releasing them
    /// </summary>
    internal class TestResourcesCollector
    {
        private bool _cleaningIsInProgress;
        private readonly string _defaultAffinityGroup;
        private string _lastAffinityGroup;
        private readonly string _defaultStorageContainerName;
        private string _lastStorageContainerName;
        private readonly string _defaultStorageServiceName;
        private string _lastStorageServiceName;
        private readonly string _defaultHostedServiceName;
        private string _lastHostedServiceName;
        private readonly string _defaultDeploymentName;
        private string _lastDeploymentName;
        private readonly string _defaultVirtualMachineName;
        private string _lastVirtualMachineName;
        private readonly string _defaultWebSiteName;
        private string _lastWebSiteName;


        // Note: for simplicity here we define list of delegates instead of creating factory
        private readonly Dictionary<AzureResourceType, Dictionary<string, object>> _resources;
        private readonly Dictionary<AzureResourceType, AzureResourceCleaner> _cleaners;

        public TestResourcesCollector()
        {
            _defaultAffinityGroup = AzureServiceConstants.DefaultAffinityGroup;
            _defaultStorageContainerName = AzureServiceConstants.DefaultContainer; // should be lower case
            _defaultStorageServiceName = AzureServiceConstants.DefaultStorageServiceName; // should be lower case
            _defaultHostedServiceName = AzureServiceConstants.DefaultHostingService; // should be lower case
            _defaultDeploymentName = AzureServiceConstants.DefaultDeploymentName; // should be lower case
            _defaultVirtualMachineName = AzureServiceConstants.DefaultVmName;
            _defaultWebSiteName = AzureServiceConstants.DefaultWebSiteName;

            _cleaners = new Dictionary<AzureResourceType, AzureResourceCleaner>
                {
                    { AzureResourceType.WebSite, CleanupWebSites },
                    { AzureResourceType.Deployment, CleanupDeployments },
                    { AzureResourceType.HostedService, CleanupHostedServices },
                    { AzureResourceType.SqlDatabase, CleanupDatabases },
                    { AzureResourceType.SqlServer, CleanupDatabaseServers },
                    { AzureResourceType.StorageBlob, CleanupBlobs },
                    { AzureResourceType.StorageContainer, CleanupContainers },
                    { AzureResourceType.StorageService, CleanupStorageServices },
                    { AzureResourceType.AffinityGroup, CleanupAffinityGroups }
                };

            _resources = new Dictionary<AzureResourceType, Dictionary<string, object>>
                {
                    { AzureResourceType.WebSite, new Dictionary<string, object>() },
                    { AzureResourceType.Deployment, new Dictionary<string, object>() },
                    { AzureResourceType.HostedService, new Dictionary<string, object>() },
                    { AzureResourceType.SqlDatabase, new Dictionary<string, object>() },
                    { AzureResourceType.SqlServer, new Dictionary<string, object>() },
                    { AzureResourceType.StorageBlob, new Dictionary<string, object>() },
                    { AzureResourceType.StorageContainer, new Dictionary<string, object>() },
                    { AzureResourceType.StorageService, new Dictionary<string, object>() },
                    { AzureResourceType.AffinityGroup, new Dictionary<string, object>() }
                };
        }

        internal void Remember(AzureResourceType type, string key, object resource)
        {
            Dictionary<string, object> typeResources;
            if (!_resources.TryGetValue(type, out typeResources))
            {
                typeResources = new Dictionary<string, object>();
                _resources.Add(AzureResourceType.AffinityGroup, typeResources);
            }

            object existing;
            if (!typeResources.TryGetValue(key, out existing))
            {
                typeResources.Add(key, resource);
            }
        }

        internal void Forget(AzureResourceType type, string key)
        {
            if (_cleaningIsInProgress) return;

            Dictionary<string, object> typeResources;
            if (!_resources.TryGetValue(type, out typeResources)) return; // nothing to forget

            object existing;
            if (typeResources.TryGetValue(key, out existing))
            {
                typeResources.Remove(key);
            }
        }

        public void CleanupResources()
        {
            TestEasyLog.Instance.StartScenario("Azure Resources clean up");

            _cleaningIsInProgress = true;
            foreach (var kv in _resources)
            {
                _cleaners[kv.Key].Invoke(kv.Key, kv.Value);
            }

            _cleaningIsInProgress = false;

            TestEasyLog.Instance.EndScenario("Clean up complete");
        }

        private void CleanupBlobs(AzureResourceType type, Dictionary<string, object> resources)
        {
            SafeExecute(() =>
                {
                    foreach (var b in resources.Values)
                    {
                        var blob = b as CloudBlockBlob;
                        if (blob == null)
                        {
                            throw new Exception(string.Format("Incorrect resource was stored in '{0}' collection", type));
                        }

                        blob.DeleteIfExists();
                    }

                    resources.Clear();
                }, type.ToString());
        }

        private void CleanupContainers(AzureResourceType type, Dictionary<string, object> resources)
        {
            SafeExecute(() =>
                {
                    foreach (var c in resources.Values)
                    {
                        var container = c as CloudBlobContainer;
                        if (container == null)
                        {
                            throw new Exception(string.Format("Incorrect resource was stored in '{0}' collection", type));
                        }

                        container.DeleteIfExists();
                    }

                    resources.Clear();
                }, type.ToString());
        }

        private void CleanupStorageServices(AzureResourceType type, Dictionary<string, object> resources)
        {
            SafeExecute(() =>
                {
                    var manager = new AzureStorageServiceManager();
                    foreach (var obj in resources.Values)
                    {
                        var service = obj as string;

                        if (service == null)
                        {
                            throw new Exception(string.Format("Incorrect resource was stored in '{0}' collection", type));
                        }

                        if (manager.GetStorageService(service) != null)
                        {
                            manager.DeleteStorageService(service);
                        }
                    }

                    resources.Clear();
                }, type.ToString());
        }

        private void CleanupAffinityGroups(AzureResourceType type, Dictionary<string, object> resources)
        {
            SafeExecute(() =>
                {
                    var manager = new AzureAffinityGroupManager();
                    foreach (var obj in resources.Values)
                    {
                        var group = obj as string;
                        if (group == null)
                        {
                            throw new Exception(string.Format("Incorrect resource was stored in '{0}' collection", type));
                        }

                        if(manager.GetAffinityGroup(group) != null)
                        {
                            manager.DeleteAffinityGroup(group);
                        }
                    }

                    resources.Clear();
                }, type.ToString());
        }

        private void CleanupDeployments(AzureResourceType type, Dictionary<string, object> resources)
        {
            SafeExecute(() =>
            {
                var manager = new AzureCloudServiceManager();
                foreach (var obj in resources.Values)
                {
                    var deployment = obj as DeploymentInfo;
                    if (deployment == null)
                    {
                        throw new Exception(string.Format("Incorrect resource was stored in '{0}' collection", type));
                    }

                    if (manager.GetDeployment(deployment.HostedService, deployment.Deployment.Name) != null)
                    {
                        manager.DeleteDeployment(deployment.HostedService, deployment.Deployment.Name);
                    }
                }

                resources.Clear();
            }, type.ToString());
        }

        private void CleanupHostedServices(AzureResourceType type, Dictionary<string, object> resources)
        {
            SafeExecute(() =>
            {
                var manager = new AzureCloudServiceManager();
                foreach (var obj in resources.Values)
                {
                    var service = obj as string;
                    if (service == null)
                    {
                        throw new Exception(string.Format("Incorrect resource was stored in '{0}' collection", type));
                    }

                    if (manager.GetHostedService(service) != null)
                    {
                        manager.DeleteHostedService(service);
                    }
                }

                resources.Clear();
            }, type.ToString());
        }

        private void CleanupWebSites(AzureResourceType type, Dictionary<string, object> resources)
        {
            SafeExecute(() =>
                {
                    var manager = new AzureWebSiteManager();
                    foreach (var obj in resources.Values)
                    {
                        var site = obj as WAML.WebSites.Models.WebSite;
                        if (site == null)
                        {
                            throw new Exception(string.Format("Incorrect resource was stored in '{0}' collection", type));
                        }

                        if(manager.WebSiteExists(site.Name))
                        {
                            manager.DeleteWebSite(site.Name);
                        }
                    }

                    resources.Clear();
                }, type.ToString());
        }

        private void CleanupDatabases(AzureResourceType type, Dictionary<string, object> resources)
        {
            SafeExecute(() =>
            {
                var manager = new AzureSqlManager();
                foreach (var obj in resources.Values)
                {
                    var database = obj as SqlAzureDatabase;
                    if (database == null)
                    {
                        throw new Exception(string.Format("Incorrect resource was stored in '{0}' collection", type));
                    }

                    if (manager.GetSqlServer(database.Server.Name, database.Server.User, database.Server.Password) != null 
                        && database.Server.DatabaseExists(database.Name))
                    {
                        database.Server.DropDatabase(database.Name);
                    }
                }

                resources.Clear();
            }, type.ToString());
        }

        private void CleanupDatabaseServers(AzureResourceType type, Dictionary<string, object> resources)
        {
            SafeExecute(() =>
                {
                    var manager = new AzureSqlManager();
                    foreach (var obj in resources.Values)
                    {
                        var server = obj as SqlAzureServer;
                        if (server == null)
                        {
                            throw new Exception(string.Format("Incorrect resource was stored in '{0}' collection", type));
                        }

                        if (manager.GetSqlServer(server.Name, server.User, server.Password) != null)
                        {
                            var databases = server.GetDatabases();

                            // if after we deleted our databases there are still more than one (master) database,
                            // it means some one reuses this server, so don't delete it
                            if (databases.Count() <= 1)
                            {
                                manager.DeleteSqlServer(server.Name);
                            }
                        }
                    }

                    resources.Clear();
                }, type.ToString());
        }

        private void SafeExecute(Action action, string objectName)
        {
            if (action == null) return;

            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                var message = e.Message;
                if (e.InnerException != null)
                {
                    message = message + ". Inner Exception: " + e.InnerException.Message;
                }

                TestEasyLog.Instance.Warning(string.Format("There was an exception while cleaning up {0}: '{1}'", objectName, message));
            }            
        }

        public string GetUniqueAffinityGroup(bool renew = false)
        {
            if (string.IsNullOrEmpty(_lastAffinityGroup) || renew)
            {
                _lastAffinityGroup = GetUniqueAzureObjectId(_defaultAffinityGroup);
            }

            return _lastAffinityGroup;
        }

        public string GetUniqueStorageServiceName(bool renew = false)
        {
            if (string.IsNullOrEmpty(_lastStorageServiceName) || renew)
            {
                _lastStorageServiceName = GetUniqueAzureObjectId(_defaultStorageServiceName);
            }

            return _lastStorageServiceName;
        }

        public string GetUniqueStorageContainerName(bool renew = false)
        {
            if (string.IsNullOrEmpty(_lastStorageContainerName) || renew)
            {
                _lastStorageContainerName = GetUniqueAzureObjectId(_defaultStorageContainerName);
            }

            return _lastStorageContainerName;
        }

        public string GetUniqueHostedServiceName(bool renew = false)
        {
            if (string.IsNullOrEmpty(_lastHostedServiceName) || renew)
            {
                _lastHostedServiceName = GetUniqueAzureObjectId(_defaultHostedServiceName);
            }

            return _lastHostedServiceName;
        }

        public string GetUniqueDeploymentName(bool renew = false)
        {
            if (string.IsNullOrEmpty(_lastDeploymentName) || renew)
            {
                _lastDeploymentName = GetUniqueAzureObjectId(_defaultDeploymentName);
            }

            return _lastDeploymentName;
        }

        public string GetUniqueVirtualMachineName(bool renew = false)
        {
            if (string.IsNullOrEmpty(_lastVirtualMachineName) || renew)
            {
                _lastVirtualMachineName = GetUniqueAzureObjectId(_defaultVirtualMachineName);
            }

            return _lastVirtualMachineName;
        }

        public string GetUniqueWebSiteName(bool renew = false)
        {
            if (string.IsNullOrEmpty(_lastWebSiteName) || renew)
            {
                _lastWebSiteName = GetUniqueAzureObjectId(_defaultWebSiteName);
            }

            return _lastWebSiteName;
        }

        private static string GetUniqueAzureObjectId(string prefix)
        {
            return prefix.ToLowerInvariant() + DateTime.Now.Ticks;
        }
    }
}
