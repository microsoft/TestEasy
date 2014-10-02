using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestEasy.Azure.Bvt
{
    [TestClass]
    public class AffinityGroupsTests
    {
        [TestInitialize]
        [TestCleanup]
        public void TestCleanup()
        {
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                if (portal.AffinityGroups.GetAffinityGroup(TestConstants.AffinityGroupName) != null)
                {
                    portal.AffinityGroups.DeleteAffinityGroup(TestConstants.AffinityGroupName);
                }
            }
        }

        [TestMethod]
        public void AffinityGroups_Create_Delete_E2E()
        {
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                portal.AffinityGroups.CreateAffinityGroup(TestConstants.AffinityGroupName);

                Assert.IsNotNull(portal.AffinityGroups.GetAffinityGroup(TestConstants.AffinityGroupName));

                portal.AffinityGroups.DeleteAffinityGroup(TestConstants.AffinityGroupName);

                Assert.IsNull(portal.AffinityGroups.GetAffinityGroup(TestConstants.AffinityGroupName));
            }
        }

        [TestMethod]
        public void AffinityGroups_Waml_Create_Delete_E2E()
        {
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                portal.AffinityGroups.CreateAffinityGroup(new Microsoft.WindowsAzure.Management.Models.AffinityGroupCreateParameters
                {
                    Name = TestConstants.AffinityGroupName,
                    Location = "West US",
                });

                Assert.IsNotNull(portal.AffinityGroups.GetAffinityGroup(TestConstants.AffinityGroupName));

                portal.AffinityGroups.DeleteAffinityGroup(TestConstants.AffinityGroupName);

                Assert.IsNull(portal.AffinityGroups.GetAffinityGroup(TestConstants.AffinityGroupName));
            }
        }

        [TestMethod]
        public void AffinityGroups_EnsureAffinityGroupExists_CreatesNewGroup()
        {
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                portal.AffinityGroups.EnsureAffinityGroupExists(TestConstants.AffinityGroupName);

                Assert.IsNotNull(portal.AffinityGroups.GetAffinityGroup(TestConstants.AffinityGroupName));
            }
        }

        [TestMethod]
        public void AffinityGroups_CleanedUpAfterTest()
        {
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                portal.AffinityGroups.CreateAffinityGroup(TestConstants.AffinityGroupName);

                Assert.IsNotNull(portal.AffinityGroups.GetAffinityGroup(TestConstants.AffinityGroupName));
            }

            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                // affinity group should have been cleaned up when previous Portal instance was disposed.
                Assert.IsNull(portal.AffinityGroups.GetAffinityGroup(TestConstants.AffinityGroupName));
            }
        }
    }
}
