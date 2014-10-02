using Microsoft.Win32;

namespace TestEasy.Core.Abstractions
{
    /// <summary>
    ///     Abstraction for making operations with registry
    /// </summary>
    public class RegistrySystem : IRegistrySystem
    {
        public string GetRegistryKeyValue(RegistryKey root, string key, string valueName)
        {
            var regKey = root.OpenSubKey(key);

            if (regKey != null)
            {
                var regValue = regKey.GetValue(valueName);

                if (regValue != null)
                {
                    return regValue.ToString();
                }
            }

            return null;
        }

        public void SetRegistryKeyValue(RegistryKey root, string key, string valueName, object value)
        {
            var regKey = root.OpenSubKey(key, true);

            if (regKey == null)
            {
                regKey = root.CreateSubKey(key);
            }

            if (regKey != null)
            {
                regKey.SetValue(valueName, value);
                regKey.Close();
            }
        }

        public void RemoveRegistryKey(RegistryKey root, string key)
        {
            root.DeleteSubKeyTree(key, false);
        }
    }
}
