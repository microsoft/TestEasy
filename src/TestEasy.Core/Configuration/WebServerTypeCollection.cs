using System.Configuration;

namespace TestEasy.Core.Configuration
{
    /// <summary>
    ///     Collection of web server type config elements
    /// </summary>
    [ConfigurationCollectionAttribute(typeof(WebServerTypeElement))]
    public sealed class WebServerTypeCollection : ConfigurationElementCollection
    {
        public WebServerTypeElement this[int index]
        {
            get
            {
                return ((WebServerTypeElement)BaseGet(index));
            }
        }

        public new WebServerTypeElement this[string entry]
        {
            get
            {
                return (WebServerTypeElement)BaseGet(entry.ToLowerInvariant());
            }
        }

        protected override string ElementName
        {
            get
            {
                return "webServerType";
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
            return new WebServerTypeElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((WebServerTypeElement)(element)).Name.ToLowerInvariant();
        }

        internal void Add(WebServerTypeElement element)
        {
            BaseAdd(element);
        }

        internal void Clear()
        {
            BaseClear();
        }
    }
}
