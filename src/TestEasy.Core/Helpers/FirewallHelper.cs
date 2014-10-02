using System.Diagnostics;
using TestEasy.Core.Abstractions;

namespace TestEasy.Core.Helpers
{
    /// <summary>
    ///     Helper API to deal with firewall
    /// </summary>
    public class FirewallHelper
    {
        internal readonly IProcessRunner ProcessRunner;

        public FirewallHelper()
            :this(AbstractionsLocator.Instance.ProcessRunner)
        {            
        }

        internal FirewallHelper(IProcessRunner processRunner)
        {
            ProcessRunner = processRunner;
        }

        public void AddProgramToFirewall(string path, string name)
        {
            RunFirewallCommand(string.Format(@"firewall add allowedprogram ""{0}"" {1} enable", path, name));
        }

        public void AddPortToFirewall(int port, string name)
        {
            RunFirewallCommand(string.Format(@"firewall add portopening tcp {0} {1}", port, name));
        }

        private void RunFirewallCommand(string command)
        {
            var startInfo = new ProcessStartInfo("netsh.exe", command) { CreateNoWindow = true, UseShellExecute = false };
            var firewall = new Process { StartInfo = startInfo };

            ProcessRunner.Start(firewall);

            ProcessRunner.WaitForExit(firewall, 60000);
        }
    }
}
