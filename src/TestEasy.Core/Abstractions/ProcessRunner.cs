using System.Diagnostics;

namespace TestEasy.Core.Abstractions
{
    /// <summary>
    ///     Abstractions for dealing with processes
    /// </summary>
    public class ProcessRunner : IProcessRunner
    {
        public bool Start(Process process)
        {
            return process.Start();
        }

        public void Stop(Process process)
        {
            process.Kill();

            process.Close();
        }

        public bool WaitForExit(Process process, int milliseconds)
        {
            return process.WaitForExit(milliseconds);
        }

        public int GetProcessExitCode(Process process)
        {
            return process.ExitCode;
        }

        public string GetProcessOutput(Process process)
        {
            return process.StandardOutput.ReadToEnd();
        }
    }
}
