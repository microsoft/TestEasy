using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.WindowsAzure.Management.Storage;
using Microsoft.WindowsAzure.Management.Storage.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using TestEasy.Azure.Helpers;
using TestEasy.Core;

namespace TestEasy.Azure
{
    /// <summary>
    ///     Contains helper methods for managing storage resources
    /// </summary>
    public class AzureStorageServiceManager
    {
        /// <summary>
        ///     Rest API client
        /// </summary>
        public IStorageManagementClient StorageManagementClient
        {
            get;
            private set;
        }

        internal AzureStorageServiceManager()
        {            
            StorageManagementClient = new StorageManagementClient(Dependencies.Subscription.Credentials, 
                new Uri(Dependencies.Subscription.CoreEndpointUrl));
        }

        /// <summary>
        ///     Create storage service
        /// </summary>
        /// <param name="storageServiceName"></param>
        /// <returns></returns>
        public StorageAccount CreateStorageService(string storageServiceName = "")
        {
            if (string.IsNullOrEmpty(storageServiceName))
            {
                // notice that we reuse our default storage service since, usually number of 
                // storage services is limited and we can't create them for each test.
                storageServiceName = Dependencies.Subscription.DefaultStorageName;
            }

            return CreateStorageService(new StorageAccountCreateParameters
                {
                    Name = storageServiceName,
                    Label = Base64EncodingHelper.EncodeToBase64String(AzureServiceConstants.DefaultLabel),
                    Description = "",
                    AffinityGroup = AzureServiceConstants.DefaultAffinityGroup,
                    GeoReplicationEnabled = false,
                });
        }

        /// <summary>
        ///     Create stirage service
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public StorageAccount CreateStorageService(StorageAccountCreateParameters input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            // storage services must use lowercase names
            input.Name = input.Name.ToLowerInvariant(); 

            var existing = GetStorageService(input.Name);
            if(existing != null)
            {
                return existing;
            }

            var affinityGroupManager = new AzureAffinityGroupManager();
            affinityGroupManager.EnsureAffinityGroupExists(input.AffinityGroup);

            TestEasyLog.Instance.Info(string.Format("Creating storage service '{0}'", input.Name));

            StorageManagementClient.StorageAccounts.CreateAsync(input, new CancellationToken()).Wait();

            existing = GetStorageService(input.Name);

            if (string.Compare(existing.Name, AzureServiceConstants.DefaultStorageServiceName,
                   StringComparison.InvariantCultureIgnoreCase) != 0) // never remember default storage service to avoid deletion
            {
                Dependencies.TestResourcesCollector.Remember(AzureResourceType.StorageService, existing.Name, existing.Name);
            }

            return existing;
        }

        /// <summary>
        ///     Update storage service
        /// </summary>
        /// <param name="storageServiceName"></param>
        /// <param name="input"></param>
        public void UpdateStorageService(string storageServiceName, StorageAccountUpdateParameters input)
        {
            TestEasyLog.Instance.Info(string.Format("Updating storage service '{0}'", storageServiceName));

            StorageManagementClient.StorageAccounts.UpdateAsync(storageServiceName, input, new CancellationToken()).Wait();
        }

        /// <summary>
        ///     Delete storage service
        /// </summary>
        /// <param name="storageServiceName"></param>
        public void DeleteStorageService(string storageServiceName)
        {
            TestEasyLog.Instance.Info(string.Format("Deleting storage service '{0}'", storageServiceName));

            StorageManagementClient.StorageAccounts.DeleteAsync(storageServiceName, new CancellationToken()).Wait();

            Dependencies.TestResourcesCollector.Forget(AzureResourceType.StorageService, storageServiceName);
        }

        /// <summary>
        ///     Check if storage service name available
        /// </summary>
        /// <param name="storageServiceName"></param>
        /// <returns></returns>
        public bool IsStorageServiceNameAvailable(string storageServiceName)
        {
            return StorageManagementClient.StorageAccounts.CheckNameAvailabilityAsync(storageServiceName, new CancellationToken()).Result.IsAvailable;
        }

        /// <summary>
        ///     Returns storage service
        /// </summary>
        /// <param name="storageServiceName"></param>
        /// <returns></returns>
        public StorageAccount GetStorageService(string storageServiceName)
        {
            try
            {
                TestEasyLog.Instance.Info(string.Format("Getting storage service '{0}'", storageServiceName));

                var result = StorageManagementClient.StorageAccounts.GetAsync(storageServiceName, new CancellationToken()).Result;

                var storageService = result.StorageAccount;
                TestEasyLog.Instance.LogObject(storageService);

                return storageService;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     Return storage service for a blob
        /// </summary>
        /// <param name="blobUri"></param>
        /// <returns></returns>
        public StorageAccount GetStorageServiceForBlob(Uri blobUri)
        {
            var storageName = blobUri.Host.Split('.').First();
            var storage = GetStorageService(storageName);
            return storage;
        }

        /// <summary>
        ///     Get list of storage services
        /// </summary>
        /// <returns></returns>
        public IList<StorageAccount> GetStorageServices()
        {
            TestEasyLog.Instance.Info("Listing storage services");

            var result = StorageManagementClient.StorageAccounts.ListAsync(new CancellationToken()).Result;

            TestEasyLog.Instance.LogObject(result.StorageAccounts);

            return result.StorageAccounts;
        }

        /// <summary>
        ///     Check if storage service exists
        /// </summary>
        /// <param name="storageServiceName"></param>
        /// <returns></returns>
        public bool StorageServiceExists(string storageServiceName)
        {
            TestEasyLog.Instance.Info(string.Format("Checking if storage service is available: '{0}'", storageServiceName));

            return GetStorageService(storageServiceName) != null;
        }

        /// <summary>
        ///     Get storage primary key
        /// </summary>
        /// <param name="storageServiceName"></param>
        /// <returns></returns>
        public string GetStoragePrimaryKey(string storageServiceName)
        {
            var result = GetStorageAccountKeys(storageServiceName);

            return result.PrimaryKey;
        }

        /// <summary>
        ///     Get storage secondary key
        /// </summary>
        /// <param name="storageServiceName"></param>
        /// <returns></returns>
        public string GetStorageSecondaryKey(string storageServiceName)
        {
            var result = GetStorageAccountKeys(storageServiceName);

            return result.SecondaryKey;
        }

        private StorageAccountGetKeysResponse GetStorageAccountKeys(string storageServiceName)
        {
            TestEasyLog.Instance.Info(string.Format("Getting storage keys for '{0}'", storageServiceName));

            var result = StorageManagementClient.StorageAccounts.GetKeysAsync(storageServiceName, new CancellationToken()).Result;
            return result;
        }

        /// <summary>
        ///     Regenerate storage service keys
        /// </summary>
        /// <param name="storageServiceName"></param>
        /// <param name="keyType"></param>
        public void RegenerateStorageServiceKeys(string storageServiceName, StorageKeyType keyType)
        {
            TestEasyLog.Instance.Info(string.Format("Regenerating storage keys for '{0}'", storageServiceName));

            StorageManagementClient.StorageAccounts.RegenerateKeysAsync(new StorageAccountRegenerateKeysParameters {
                KeyType = keyType,
                Name = storageServiceName,
            }, new CancellationToken()).Wait();
        }

        /// <summary>
        ///     Returns storage service connection string
        /// </summary>
        /// <param name="storageServiceName"></param>
        /// <returns></returns>
        public string GetStorageServiceConnectionString(string storageServiceName)
        {
            var primaryStorageKey = GetStoragePrimaryKey(storageServiceName);

            return string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}",
                storageServiceName, primaryStorageKey);
        }

        /// <summary>
        ///     Create storage container.
        ///     Note: storage container name should be in lower case
        /// </summary>
        /// <param name="storageServiceName"></param>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public CloudBlobContainer CreateContainer(string storageServiceName, string containerName = "")
        {
            return CreateContainer(GetStorageService(storageServiceName.ToLowerInvariant()), containerName);
        }

        /// <summary>
        ///     Create storage container.
        ///     Note: storage container name should be in lower case
        /// </summary>
        /// <param name="service"></param>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public CloudBlobContainer CreateContainer(StorageAccount service, string containerName = "")
        {
            return service.CreateContainer(containerName);
        }

        /// <summary>
        ///     Create storage blob.
        ///     Note: storage container name should be in lower case
        /// </summary>
        /// <param name="storageServiceName"></param>
        /// <param name="blobName"></param>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public CloudBlockBlob CreateBlob(string storageServiceName, string blobName, string containerName = "")
        {
            return CreateBlob(GetStorageService(storageServiceName.ToLowerInvariant()), blobName, containerName);
        }

        /// <summary>
        ///     Create storage blob.
        ///     Note: storage container name should be in lower case
        /// </summary>
        /// <param name="service"></param>
        /// <param name="blobName"></param>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public CloudBlockBlob CreateBlob(StorageAccount service, string blobName, string containerName = "")
        {
            return service.CreateBlob(blobName, containerName);
        }
    }
}