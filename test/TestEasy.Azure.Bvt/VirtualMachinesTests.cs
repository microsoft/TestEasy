using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestEasy.Azure.Bvt
{
    [TestClass]
    public class VirtualMachinesTests
    {
        [TestMethod]
        public void AzureVirtualMachines_GetListOfAllAvailableOsImages_Test()
        {
            // Note: !!! it is recommended though to use "using" statement since it would call Dispose even in 
            // the case of exception, which would ensure all resources cleaned up.
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                var oses = portal.VirtualMachines.GetOsImages();
                Assert.IsTrue(oses.Any());
            }
        }

    }
}
