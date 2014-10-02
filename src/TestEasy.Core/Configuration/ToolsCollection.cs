using System.Configuration;

namespace TestEasy.Core.Configuration
{
    /// <summary>
    ///     Collection of tools config elements
    /// </summary>
    [ConfigurationCollectionAttribute(typeof(ToolElement))]
    public sealed class ToolsCollection : ConfigurationElementCollection
    {
        public ToolElement this[int index]
        {
            get
            {
                return ((ToolElement)BaseGet(index));
            }
        }

        public new ToolElement this[string entry]
        {
            get
            {
                return (ToolElement)BaseGet(entry.ToLowerInvariant());
            }
        }

        protected override string ElementName
        {
            get
            {
                return "tool";
            }
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMap;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ToolElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ToolElement)(element)).Name.ToLowerInvariant();
        }

        internal void Add(ToolElement element)
        {
            BaseAdd(element);
        }

        internal void Clear()
        {
            BaseClear();
        }
    }
}
