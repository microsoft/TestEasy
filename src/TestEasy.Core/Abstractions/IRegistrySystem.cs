using Microsoft.Win32;

namespace TestEasy.Core.Abstractions
{
    public interface IRegistrySystem
    {
        string GetRegistryKeyValue(RegistryKey root, string key, string valueName);
        void SetRegistryKeyValue(RegistryKey root, string key, string valueName, object value);
        void RemoveRegistryKey(RegistryKey root, string key);
    }
}
