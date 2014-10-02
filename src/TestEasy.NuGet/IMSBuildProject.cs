using System.Collections.Generic;

namespace TestEasy.NuGet
{
    public interface IMSBuildProject
    {
        string FullPath { get; }

        void AddReference(string assemblyName);
        void AddReference(string assemblyName, string hintPath);
        bool ReferenceExists(string assemblyName);

        bool ItemExists(string itemType, string name);
        bool AnyItemExists(string name);
        void RemoveItem(string itemType, string name);

        Dictionary<string, string> GetItemsWithMetadataProperty(string itemType, string metadataPropertyName);

        string GetPropertyValue(string propertyName);

        void Save();
    }
}
