using System.Diagnostics;

namespace TestEasy.Core.Abstractions
{
    public interface IProcessRunner
    {
        bool Start(Process process);
        void Stop(Process process);
        bool WaitForExit(Process process, int milliseconds);
        int GetProcessExitCode(Process process);
        string GetProcessOutput(Process process);
    }
}
