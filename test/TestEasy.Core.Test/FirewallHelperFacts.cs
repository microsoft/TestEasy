using System.Diagnostics;
using Xunit;
using Moq;
using TestEasy.Core.Abstractions;
using TestEasy.Core.Helpers;

namespace TestEasy.Core.Test
{
    public class FirewallHelperFacts
    {
        [Fact]
        public void AddProgramToFirewallFact()
        {
            // Arrange
            var mockProcessRunner = new Mock<IProcessRunner>(MockBehavior.Strict);
            mockProcessRunner.Setup(m => m.Start(It.Is<Process>(
                                    p => p.StartInfo.FileName == "netsh.exe" && p.StartInfo.Arguments == @"firewall add allowedprogram ""mytool.exe"" TestEasyTool enable")))
                                .Returns(true);
            mockProcessRunner.Setup(m => m.WaitForExit(It.Is<Process>(
                                    p => p.StartInfo.FileName == "netsh.exe" && p.StartInfo.Arguments == @"firewall add allowedprogram ""mytool.exe"" TestEasyTool enable"), 60000))
                                .Returns(true);

            // Act     
            // Assert  
            var helper = new FirewallHelper(mockProcessRunner.Object);
            helper.AddProgramToFirewall("mytool.exe", "TestEasyTool");
        }

        [Fact]
        public void AddPortToFirewallFact()
        {
            // Arrange
            var mockProcessRunner = new Mock<IProcessRunner>(MockBehavior.Strict);
            mockProcessRunner.Setup(m => m.Start(It.Is<Process>(
                                    p => p.StartInfo.FileName == "netsh.exe" && p.StartInfo.Arguments == @"firewall add portopening tcp 444 TestEasyTool")))
                                .Returns(true);
            mockProcessRunner.Setup(m => m.WaitForExit(It.Is<Process>(
                                    p => p.StartInfo.FileName == "netsh.exe" && p.StartInfo.Arguments == @"firewall add portopening tcp 444 TestEasyTool"), 60000))
                                .Returns(true);

            // Act     
            // Assert  
            var helper = new FirewallHelper(mockProcessRunner.Object);
            helper.AddPortToFirewall(444, "TestEasyTool");
        }        
    }
}
