using System;
using System.IO;
using System.Xml;
using TestEasy.Core.Abstractions;
using System.Linq;

namespace TestEasy.Core.Helpers
{
    /// <summary>
    ///     Helper APIs to deal with xml 
    /// </summary>
    public class XmlHelper
    {
        internal readonly IFileSystem FileSystem;

        /// <summary>
        ///     ctor
        /// </summary>
        public XmlHelper()
            :this(new FileSystem())
        {            
        }

        /// <summary>
        ///     ctor
        /// </summary>
        /// <param name="fileSystem"></param>
        internal XmlHelper(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }

        /// <summary>
        ///     Merge two xml files into one
        /// </summary>
        /// <param name="sourceXml"></param>
        /// <param name="overrideXml"></param>
        /// <param name="collectionElementNames"></param>
        /// <returns></returns>
        public string MergeXmlFiles(string sourceXml, string overrideXml, string[] collectionElementNames = null)
        {
            if (string.IsNullOrEmpty(sourceXml))
            {
                throw new ArgumentNullException("sourceXml");
            }

            if (string.IsNullOrEmpty(overrideXml))
            {
                throw new ArgumentNullException("overrideXml");
            }

            if (!FileSystem.FileExists(sourceXml))
            {
                throw new FileNotFoundException(string.Format("File does not exist '{0}'.", sourceXml));
            }

            if (!FileSystem.FileExists(overrideXml))
            {
                throw new FileNotFoundException(string.Format("File does not exist '{0}'.", overrideXml));
            }

            return MergeXml(FileSystem.FileReadAllText(sourceXml), FileSystem.FileReadAllText(overrideXml), collectionElementNames);
        }

        /// <summary>
        ///  Merge two xml strings into one
        /// </summary>
        /// <param name="sourceXml"></param>
        /// <param name="overrideXml"></param>
        /// <param name="collectionElementNames"></param>
        /// <returns></returns>
        public string MergeXml(string sourceXml, string overrideXml, string[] collectionElementNames = null)
        {
            if (string.IsNullOrEmpty(sourceXml))
            {
                return overrideXml;
            }

            if (string.IsNullOrEmpty(overrideXml))
            {
                return sourceXml;
            }

            var sourceDoc = new XmlDocument();
            sourceDoc.LoadXml(sourceXml);

            var overrideDoc = new XmlDocument();
            overrideDoc.LoadXml(overrideXml);

            if (sourceDoc.DocumentElement != null && overrideDoc.DocumentElement != null)
            {
                XmlElement sourceElement;
                XmlElement sourceParent;
                if (sourceDoc.DocumentElement.Name.Equals(overrideDoc.DocumentElement.Name,
                                                          StringComparison.InvariantCultureIgnoreCase))
                {
                    sourceParent = null;
                    sourceElement = sourceDoc.DocumentElement;
                }
                else
                {
                    sourceParent = sourceDoc.DocumentElement;
                    sourceElement = null;
                }

                MergeXmlRecursive(sourceDoc, sourceParent, sourceElement, overrideDoc.DocumentElement, collectionElementNames ?? new [] { "add" });                   
            }

            return sourceDoc.InnerXml;
        }

        private void MergeXmlRecursive(XmlDocument sourceDoc, XmlElement sourceParent, XmlElement sourceElement, XmlElement overrideElement, string[] collectionElementNames)
        {
            if (sourceElement == null)
            {
                // there no such elemement in original xml, just add whole inner xml from override element
                sourceParent.InnerXml += overrideElement.OuterXml;
                return;
            }

            // add all attributes first
            foreach (XmlAttribute overrideAttribute in overrideElement.Attributes)
            {
                if (!sourceElement.HasAttribute(overrideAttribute.Name))
                {
                    sourceElement.Attributes.Append(sourceDoc.CreateAttribute(overrideAttribute.Name));
                }

                sourceElement.Attributes[overrideAttribute.Name].Value = overrideAttribute.Value;
            }

            // determine if element is a collection
            if (IsCollectionElement(overrideElement, collectionElementNames))
            {
                // just merge inner xmls
                sourceElement.InnerXml += overrideElement.InnerXml;
                return;
            }

            // now go recursive for all children in overrideElement 
            foreach (XmlElement child in overrideElement.ChildNodes)
            {
                var sourceChildren = sourceElement.GetElementsByTagName(child.Name);

                MergeXmlRecursive(sourceDoc, sourceElement, sourceChildren.Count > 0 ? (XmlElement)sourceChildren[0] : null, child, collectionElementNames);
            }
        }

        private static bool IsCollectionElement(XmlElement element, string[] collectionElementNames)
        {
            foreach (XmlElement child in element.ChildNodes)
            {
                if (collectionElementNames.Any(e => e.Equals(child.Name, StringComparison.InvariantCultureIgnoreCase)))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Specifies if xml file containes xpath element
        /// </summary>
        /// <param name="file"></param>
        /// <param name="elementXpath"></param>
        /// <returns></returns>
        public bool FileContainsElement(string file, string elementXpath)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException("file");
            }

            return XmlContainsElement(FileSystem.FileReadAllText(file), elementXpath);
        }

        /// <summary>
        ///     Specifies if xml string contains xpath element
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="elementXpath"></param>
        /// <returns></returns>
        public bool XmlContainsElement(string xml, string elementXpath)
        {
            if (xml == null)
            {
                throw new ArgumentNullException("xml");
            }
            if(elementXpath == null)
            {
                throw new ArgumentNullException("elementXpath");
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            return ContainsElement(doc, elementXpath);
        }

        private static bool ContainsElement(XmlDocument doc, string xpath)
        {
            var nav = doc.CreateNavigator();

            return nav.Select(xpath).Count > 0;
        }
    }
}
