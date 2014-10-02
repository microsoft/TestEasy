using System.Threading;
using Microsoft.WindowsAzure.Management.Storage.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using TestEasy.Core;

namespace TestEasy.Azure
{
    /// <summary>
    ///     Storage service extensions
    /// </summary>
    public static class StorageServiceExtensions
    {
        /// <summary>
        ///     Constructs connection string for storage service
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public static string GetConnectionString(this StorageAccount service)
        {
            var manager = new AzureStorageServiceManager();
            return manager.GetStorageServiceConnectionString(service.Name);
        }

        /// <summary>
        ///     Create a storage container
        /// </summary>
        /// <param name="service"></param>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static CloudBlobContainer CreateContainer(this StorageAccount service, string containerName = "")
        {
            var connectionString = GetConnectionString(service);

            // Retrieve storage account from connection string.
            var storageAccount = CloudStorageAccount.Parse(connectionString);

            // Create the blob client.
            var blobClient = storageAccount.CreateCloudBlobClient();

            if (string.IsNullOrEmpty(containerName))
            {
                // we need to use unique names here, or else we might try to reuse an existing container
                // which is still being deleted by a previous test cleanup/delete operation.
                containerName = Dependencies.TestResourcesCollector.GetUniqueStorageContainerName(true);
            }

            // Retrieve reference to a previously created container.
            var container = blobClient.GetContainerReference(containerName);

            // there possible exception "Conflict" 409 which can be caused by independent on the code reasosns in developer environment,
            // thus we want to make it more robust and add retries.
            bool repeat;
            var retryCount = 0;
            do
            {
                try
                {
                    repeat = false;
                    retryCount++;
                    container.CreateIfNotExists();
                }
                catch (StorageException e)
                {
                    repeat = true;
                    TestEasyLog.Instance.Warning(string.Format("Failed to create a container '{0}'. Message: '{1}', Status code: '{2}', Status Message: '{3}'", containerName, e.Message, e.RequestInformation.HttpStatusCode, e.RequestInformation.HttpStatusMessage));
                    Thread.Sleep(2000);
                }

            } while (repeat && retryCount < 5);

            Dependencies.TestResourcesCollector.Remember(AzureResourceType.StorageContainer, container.Name, container);

            return container;
        }

        /// <summary>
        ///     Returns storage container
        /// </summary>
        /// <param name="service"></param>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static CloudBlobContainer GetContainer(this StorageAccount service, string containerName)
        {
            var storageAccount = CloudStorageAccount.Parse(GetConnectionString(service));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);

            return container;
        }

        /// <summary>
        ///     Create a storage blob
        /// </summary>
        /// <param name="service"></param>
        /// <param name="blobName"></param>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static CloudBlockBlob CreateBlob(this StorageAccount service, string blobName, string containerName = "")
        {
            // Retrieve reference to a previously created container.
            var container = string.IsNullOrEmpty(containerName) ? CreateContainer(service) : GetContainer(service, containerName);
            var blockBlob = container.GetBlockBlobReference(blobName);

            Dependencies.TestResourcesCollector.Remember(AzureResourceType.StorageBlob, blockBlob.Name, blockBlob);

            return blockBlob;
        }

        /// <summary>
        ///     Returns storage blobs
        /// </summary>
        /// <param name="service"></param>
        /// <param name="blobName"></param>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static CloudBlockBlob GetBlob(this StorageAccount service, string blobName, string containerName = "")
        {
            // Retrieve reference to a previously created container.
            var container = string.IsNullOrEmpty(containerName) ? CreateContainer(service) : GetContainer(service, containerName);
            return container.GetBlockBlobReference(blobName);
        }
    }
}
