using System;
using System.Linq;
using System.Xml.Linq;
using TestEasy.Core.Abstractions;

namespace TestEasy.WebServer
{
    /// <summary>
    /// Helper class that simplifies applicationHost.config editing for IISExpress.
    /// 
    /// NOTE: At this moment it works only with default website with id = 1, we will extend it when its needed
    /// 
    /// </summary>
    class IisExpressConfig
    {
        private const string DefaultSiteId = "1";

        private readonly IFileSystem _fileSystem;
        private string _applicationHostConfig;
        private XDocument _xdocument;
        private XElement _xsite;

        /// <summary>
        /// If one needs to perform extra xml manipulations without changing code,
        /// this property should be used
        /// </summary>
        public XDocument Root
        {
            get
            {
                return _xdocument;

            }
        }

        /// <summary>
        ///     ctor
        /// </summary>
        /// <param name="applicationHostConfig"></param>
        public IisExpressConfig(string applicationHostConfig)
            : this(applicationHostConfig, new FileSystem())
        {
        }

        internal IisExpressConfig(string applicationHostConfig, IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;

            LoadSchema(applicationHostConfig);
        }

        /// <summary>
        ///     Load xml schema from given applicationhost.config
        /// </summary>
        /// <param name="applicationHostConfig"></param>
        public void LoadSchema(string applicationHostConfig)
        {
            _applicationHostConfig = applicationHostConfig;

            if (!_fileSystem.FileExists(_applicationHostConfig))
            {
                throw new Exception(string.Format("ApplicationHost.config file was not found: '{0}'", _applicationHostConfig));
            }

            _xdocument = _fileSystem.LoadXDocumentFromFile(_applicationHostConfig); // TD: add overload for IFileSystem
            if (_xdocument == null)
            {
                throw new Exception(string.Format("Somethng went wrong while loading xml schema from config file: '{0}'", _applicationHostConfig));
            }

            _xsite = GetSite();
        }

        /// <summary>
        ///     Add application to applicationhost.config
        /// </summary>
        /// <param name="name"></param>
        /// <param name="physicalPath"></param>
        /// <returns></returns>
        public XElement AddApplication(string name, string physicalPath)
        {
            var appVirtualPath = "/" + name;

            var xapplications = _xsite.Descendants("application").ToList();
            if (xapplications.Any(a => a.Attribute("path").Value.Equals(appVirtualPath, StringComparison.InvariantCultureIgnoreCase)))
            {
                xapplications.Where(a => a.Attribute("path").Value.Equals(appVirtualPath, StringComparison.InvariantCultureIgnoreCase)).Remove();
            }

            var xNewApp = new XElement("application", new XAttribute("path", appVirtualPath));
            _xsite.Add(xNewApp);

            var xNewVirualDirectory = new XElement("virtualDirectory", new XAttribute("path", "/"), new XAttribute("physicalPath", physicalPath));
            xNewApp.Add(xNewVirualDirectory);

            return xNewApp;
        }

        /// <summary>
        ///     Return a property for web application in applicationhost.config
        /// </summary>
        /// <param name="name"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public string GetApplicationProperty(string name, string propertyName)
        {
            var xapplications =
                _xsite.Descendants("application").Where(a => a.Attribute("path").Value == "/" + name.Trim('/')).ToList();
            if (!xapplications.Any()) return null;
            
            var xapplication = xapplications.First();
            var xproperty = xapplication.Attribute(propertyName);
            if (xproperty == null)
            {
                var xvirtualDirectories = xapplication.Descendants("virtualDirectory").ToList();
                if (xvirtualDirectories.Any())
                {
                    var xvd = xvirtualDirectories.First();
                    xproperty = xvd.Attribute(propertyName);
                }
            }

            return xproperty == null ? null : xproperty.Value;
        }

        /// <summary>
        ///     Remove web application from applicationhost.config
        /// </summary>
        /// <param name="name"></param>
        public void RemoveApplication(string name)
        {
            var appVirtualPath = "/" + name;

            var xapplications = _xsite.Descendants("application").ToList();
            if (xapplications.Any(a => a.Attribute("path").Value.Equals(appVirtualPath, StringComparison.InvariantCultureIgnoreCase)))
            {
                xapplications.Where(a => a.Attribute("path").Value.Equals(appVirtualPath, StringComparison.InvariantCultureIgnoreCase)).Remove();
            }
        }

        /// <summary>
        ///     Remove all web applications from applicationhost.config
        /// </summary>
        public void RemoveAllApplications()
        {
            var xapplications = _xsite.Descendants("application");
            xapplications.Remove();
        }

        /// <summary>
        ///     Add binding for default web site in applicationhost.config
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="bindingInformation"></param>
        /// <returns></returns>
        public XElement AddBinding(string protocol, string bindingInformation)
        {
            var xbindings = EnsureElementExists("bindings", _xsite);
            
            var xbindingList = xbindings.Descendants("binding").ToList();
            if (xbindingList.Any(a => a.Attribute("protocol").Value.Equals(protocol, StringComparison.InvariantCultureIgnoreCase)))
            {
                xbindingList.Where(a => a.Attribute("protocol").Value.Equals(protocol, StringComparison.InvariantCultureIgnoreCase)).Remove();
            }

            var xNewBinding = new XElement("binding", new XAttribute("protocol", protocol), new XAttribute("bindingInformation", bindingInformation));
            xbindings.Add(xNewBinding);

            return xNewBinding;
        }

        /// <summary>
        ///     Remove binding from default web site in applicationhost.config
        /// </summary>
        /// <param name="protocol"></param>
        public void RemoveBinding(string protocol)
        {
            var xbindingsList = _xsite.Descendants("bindings").ToList();
            if (xbindingsList.Any())
            {
                var xbindings = xbindingsList.First();
                var xbindingList = xbindings.Descendants("binding").ToList();
                if (xbindingList.Any(a => a.Attribute("protocol").Value.Equals(protocol, StringComparison.InvariantCultureIgnoreCase)))
                {
                    xbindingList.Where(a => a.Attribute("protocol").Value.Equals(protocol, StringComparison.InvariantCultureIgnoreCase)).Remove();
                }
            }            
        }

        /// <summary>
        ///     Add application pool to applicationhost.config
        /// </summary>
        /// <param name="name"></param>
        /// <param name="managedRuntimeVersion"></param>
        /// <param name="managedPipelineMode"></param>
        /// <param name="identityType"></param>
        /// <returns></returns>
        public XElement AddApplicationPool(string name, string managedRuntimeVersion = "v4.0", string managedPipelineMode = "Integrated", string identityType = "ApplicationPoolIdentity")
        {
            var xAppPools = EnsureElementExists("system.applicationHost/applicationPools");

            var xExistingAppPools = xAppPools.Descendants("add").ToList();
            if (xExistingAppPools.Any(a => a.Attribute("name").Value.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                xExistingAppPools.Where(a => a.Attribute("name").Value.Equals(name, StringComparison.InvariantCultureIgnoreCase)).Remove();
            }

            var xNewAppPool = new XElement("add", 
                new XAttribute("name", name), 
                new XAttribute("managedRuntimeVersion", managedRuntimeVersion),
                new XAttribute("managedPipelineMode", managedPipelineMode));

            var xNewIdentityType = new XElement("processModel", new XAttribute("identityType", identityType));
            xNewAppPool.Add(xNewIdentityType);

            xAppPools.Add(xNewAppPool);

            return xNewAppPool;
        }

        /// <summary>
        ///     Set default application pool in applicationhost.config
        /// </summary>
        /// <param name="name"></param>
        public void SetDefaultApplicationPool(string name)
        {
            var xApplicationDefaults = EnsureElementExists("system.applicationHost/sites/applicationDefaults");
            var xApplicationPool = xApplicationDefaults.Attribute("applicationPool");
            if (xApplicationPool == null)
            {
                xApplicationDefaults.Add(new XAttribute("applicationPool", name));
            }
            else
            {
                xApplicationPool.Value = name;
            }
        }

        /// <summary>
        ///     Save changes to applicationhost.config
        /// </summary>
        public void StoreSchema()
        {
            if (_xdocument == null) return;

            _fileSystem.StoreXDocumentToFile(_xdocument, _applicationHostConfig);
        }

        private XElement GetSite(string id = DefaultSiteId)
        {
            if (_xdocument.Document == null)
            {
                throw new Exception("Incorrect xml schema in applicationhost.config.");
            }

            var xDefaultSites = _xdocument.Document.Descendants("site").Where(s => s.Attribute("id").Value == id).ToList();
            if (xDefaultSites.Any())
            {
                return xDefaultSites.First();
            }

            throw new Exception(string.Format("Site with id='{0}' was not found in '{1}'", id, _applicationHostConfig));
        }

        private XElement EnsureElementExists(string path, XElement startElement = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(string.Format("Xml element path can not be null or empty."));
            }

            // expecting to start from the root
            var hierarchy = path.Split('/');

            if (_xdocument.Document == null)
            {
                throw new Exception("Incorrect xml schema in applicationhost.config.");
            }        

            var currentElement = startElement ?? _xdocument.Document.Root;
            if (currentElement == null)
            {
                throw new ArgumentException("startElement can not be null.");
            }

            foreach (var elementName in hierarchy)
            {
                var elementsList = currentElement.Descendants(elementName).ToList();
                if (elementsList.Any())
                {
                    currentElement = elementsList.First();
                }
                else
                {
                    var xNewElement = new XElement(elementName);
                    currentElement.Add(xNewElement);

                    currentElement = xNewElement;
                }
            }

            return currentElement;
        }
    }
}
