using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Management.Compute.Models;

namespace TestEasy.Azure.Bvt
{
    [TestClass]
    public class CloudServicesTests
    {
        [TestInitialize]
        [TestCleanup]
        public void TestCleanup()
        {
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                if (portal.CloudServices.GetHostedService(TestConstants.HostedServiceName) != null)
                {
                    portal.CloudServices.DeleteHostedService(TestConstants.HostedServiceName);
                }
            }
        }


        [TestMethod]
        [DeploymentItem(@"AssetFiles\SamplePackage.cspkg")]
        [DeploymentItem(@"AssetFiles\SampleServiceConfiguration.Cloud.cscfg")]
        public void AzureCloudServices_FastWebRoleCreate_Test()
        {
            var packagePath = Path.GetFullPath("SamplePackage.cspkg");
            var configPath = Path.GetFullPath("SampleServiceConfiguration.Cloud.cscfg");

            // Note: !!! it is recommended though to use "using" statement since it would call Dispose even in 
            // the case of exception, which would ensure all resources cleaned up.
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                var webRole = portal.CloudServices.DeployRole(packagePath, configPath);

                // wait for webRole to populate
                portal.CloudServices.WaitForRoleToRespond(webRole, 5000 /* increase timeout here to 600000 in real tests */);

                // now remove WebRole
                portal.CloudServices.DeleteRole(webRole);
                Assert.IsNull(portal.CloudServices.GetDeployment(webRole.HostedService, webRole.Deployment.Name));
            }
        }

        [TestMethod]
        public void AzureCloudServices_CreateHostedServiceFromWaml_EnsureAffinityGroupExists()
        {
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                if(portal.AffinityGroups.GetAffinityGroup(TestConstants.AffinityGroupName) != null)
                {
                    portal.AffinityGroups.DeleteAffinityGroup(TestConstants.AffinityGroupName);
                }

                portal.CloudServices.CreateHostedService(new HostedServiceCreateParameters
                    {
                        ServiceName = TestConstants.HostedServiceName,
                        AffinityGroup = TestConstants.AffinityGroupName,
                    });

                Assert.IsNotNull(portal.AffinityGroups.GetAffinityGroup(TestConstants.AffinityGroupName));
            }
        }

        [TestMethod]
        public void AzureCloudServices_CreateHostedService_ReuseExisting()
        {
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                // first, create an affinity group in a different location
                portal.AffinityGroups.CreateAffinityGroup(new Microsoft.WindowsAzure.Management.Models.AffinityGroupCreateParameters
                    {
                        Name = "bvtAffinityGroupEast",
                        Location = "East US",
                    });

                portal.CloudServices.CreateHostedService(new HostedServiceCreateParameters
                    {
                        ServiceName = TestConstants.HostedServiceName,
                        AffinityGroup = "bvtAffinityGroupEast",
                    });

                // now "create" the duplicate, which will reuse the existing
                portal.CloudServices.CreateHostedService(new HostedServiceCreateParameters
                {
                    ServiceName = TestConstants.HostedServiceName,
                    AffinityGroup = "bvtAffinityGroupWest",
                });

                var service = portal.CloudServices.GetHostedService(TestConstants.HostedServiceName);

                Assert.AreEqual("bvtAffinityGroupEast", service.Properties.AffinityGroup, true);
            }
        }

        [TestMethod]
        public void AzureCloudServices_CleanupHostedServicesAfterTest()
        {
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                portal.CloudServices.CreateHostedService(TestConstants.HostedServiceName);

                Assert.IsNotNull(portal.CloudServices.GetHostedService(TestConstants.HostedServiceName));
            }

            // verify hosted service was cleaned up
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                Assert.IsNull(portal.CloudServices.GetHostedService(TestConstants.HostedServiceName));
            }
        }

        [TestMethod]
        [DeploymentItem(@"AssetFiles\SamplePackage.cspkg")]
        [DeploymentItem(@"AssetFiles\SampleServiceConfiguration.Cloud.cscfg")]
        public void AzureCloudServices_CreateDeployment_ProductionSlot()
        {
            var packagePath = Path.GetFullPath("SamplePackage.cspkg");
            var configPath = Path.GetFullPath("SampleServiceConfiguration.Cloud.cscfg");

            // Note: !!! it is recommended though to use "using" statement since it would call Dispose even in 
            // the case of exception, which would ensure all resources cleaned up.
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                var webRole = portal.CloudServices.DeployRole(packagePath, configPath);

                // wait for webRole to populate
                portal.CloudServices.WaitForRoleToRespond(webRole, 5000 /* increase timeout here to 600000 in real tests */);

                var deployment = portal.CloudServices.GetDeploymentBySlot(webRole.HostedService, DeploymentSlot.Production);

                Assert.IsNotNull(deployment);
            }
        }

        [TestMethod]
        [DeploymentItem(@"AssetFiles\SamplePackage.cspkg")]
        [DeploymentItem(@"AssetFiles\SampleServiceConfiguration.Cloud.cscfg")]
        public void AzureCloudServices_CreateDeployment_StagingSlot()
        {
            var packagePath = Path.GetFullPath("SamplePackage.cspkg");
            var configPath = Path.GetFullPath("SampleServiceConfiguration.Cloud.cscfg");

            // Note: !!! it is recommended though to use "using" statement since it would call Dispose even in 
            // the case of exception, which would ensure all resources cleaned up.
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                var webRole = portal.CloudServices.DeployRole(packagePath, configPath, deploymentSlot: DeploymentSlot.Staging);

                // wait for webRole to populate
                portal.CloudServices.WaitForRoleToRespond(webRole, 5000 /* increase timeout here to 600000 in real tests */);

                var deployment = portal.CloudServices.GetDeploymentBySlot(webRole.HostedService, DeploymentSlot.Staging);

                Assert.IsNotNull(deployment);
            }
        }

        [TestMethod]
        [DeploymentItem(@"AssetFiles\SamplePackage.cspkg")]
        [DeploymentItem(@"AssetFiles\SampleServiceConfiguration.Cloud.cscfg")]
        public void AzureCloudServices_SwapDeployment_StagingToProduction()
        {
            var packagePath = Path.GetFullPath("SamplePackage.cspkg");
            var configPath = Path.GetFullPath("SampleServiceConfiguration.Cloud.cscfg");

            // Note: !!! it is recommended though to use "using" statement since it would call Dispose even in 
            // the case of exception, which would ensure all resources cleaned up.
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                var webRole = portal.CloudServices.DeployRole(packagePath, configPath, deploymentSlot: DeploymentSlot.Staging);

                // wait for webRole to populate
                portal.CloudServices.WaitForRoleToRespond(webRole, 5000 /* increase timeout here to 600000 in real tests */);

                var deployment = portal.CloudServices.GetDeploymentBySlot(webRole.HostedService, DeploymentSlot.Staging);
                Assert.IsNotNull(deployment);

                portal.CloudServices.SwapDeployment(webRole.HostedService, new DeploymentSwapParameters
                {
                    SourceDeployment = webRole.Deployment.Name,
                });

                deployment = portal.CloudServices.GetDeploymentBySlot(webRole.HostedService, DeploymentSlot.Production);
                Assert.IsNotNull(deployment);
                deployment = portal.CloudServices.GetDeploymentBySlot(webRole.HostedService, DeploymentSlot.Staging);
                Assert.IsNull(deployment);
            }
        }
    }
}
