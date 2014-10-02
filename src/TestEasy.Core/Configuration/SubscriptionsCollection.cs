using System.Configuration;

namespace TestEasy.Core.Configuration
{
    /// <summary>
    ///     Collection of tools config elements
    /// </summary>
    [ConfigurationCollectionAttribute(typeof(SubscriptionElement))]
    public sealed class SubscriptionsCollection : ConfigurationElementCollection
    {
        public SubscriptionElement this[int index]
        {
            get
            {
                return ((SubscriptionElement)BaseGet(index));
            }
        }

        public new SubscriptionElement this[string entry]
        {
            get
            {
                return (SubscriptionElement)BaseGet(entry.ToLowerInvariant());
            }
        }

        protected override string ElementName
        {
            get
            {
                return "subscription";
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
            return new SubscriptionElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((SubscriptionElement)(element)).Name.ToLowerInvariant();
        }

        internal void Add(SubscriptionElement element)
        {
            BaseAdd(element);
        }

        internal void Clear()
        {
            BaseClear();
        }
    }
}
