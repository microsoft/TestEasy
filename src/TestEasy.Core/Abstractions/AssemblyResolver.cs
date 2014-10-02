using System;
using System.Globalization;
using System.Reflection;

namespace TestEasy.Core.Abstractions
{
    /// <summary>
    ///     Abstraction for resolving assembly full and partial names
    /// </summary>
    public class AssemblyResolver : IAssemblyResolver
    {
        // Keys taken from the 4.0 RedistList. 
        private static readonly string[] KnownPublicKeys = { "b03f5f7f11d50a3a", "b77a5c561934e089", "31bf3856ad364e35" };

        private static Version DefaultTargetFrameworkVersion
        {
            get
            {
                return typeof(string).Assembly.GetName().Version;
            }
        }

        public string ResolvePartialAssemblyName(string partialName)
        {
            foreach (var key in KnownPublicKeys)
            {
                var assemblyFullName = String.Format(CultureInfo.InvariantCulture, "{0}, Version={1}, Culture=neutral, PublicKeyToken={2}",
                    partialName, DefaultTargetFrameworkVersion, key);

                try
                {
                    Assembly.Load(assemblyFullName);
                    // Assembly.Load throws a FileNotFoundException if the assembly name cannot be resolved. If we managed to successfully locate the assembly, return it.
                    return assemblyFullName;
                }
                catch
                {
                    // Do nothing. We don't want to throw from this method.
                }
            }
            return null;
        }
    }
}
