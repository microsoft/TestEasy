using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestEasy.Azure.Bvt
{
    [TestClass]
    public class StorageServicesTests
    {
        [TestMethod]
        public void AzureStorageServices_CleanupOnDisposal()
        {
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                portal.Storage.CreateStorageService(TestConstants.StorageServiceName);
            }

            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                var service = portal.Storage.GetStorageService(TestConstants.StorageServiceName);
                Assert.IsNull(service);
            }

        }
    }
}
