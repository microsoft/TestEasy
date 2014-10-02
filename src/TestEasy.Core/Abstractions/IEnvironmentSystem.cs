using System;

namespace TestEasy.Core.Abstractions
{
    public interface IEnvironmentSystem
    {
        string ExpandEnvironmentVariables(string environmentVariable);
        int GetNextAvailablePort(int usedPort);
        Version OSVersion { get; }
        string MachineName { get; }
        bool Is64BitOperatingSystem { get; }
    }
}
