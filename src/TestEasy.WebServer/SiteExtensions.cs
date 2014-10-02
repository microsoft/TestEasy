using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Web.Administration;

namespace TestEasy.WebServer
{
    /// <summary>
    ///     Extensions for Microsoft.Web.Administration.Site
    /// </summary>
    public static class SiteExtensions
    {
        /// <summary>
        ///     Return HTTP virtual path for the site
        /// </summary>
        /// <param name="site"></param>
        /// <param name="hostName"></param>
        /// <returns></returns>
        public static string GetHttpVirtualPath(this Site site, string hostName)
        {
            return GetVirtualPath(site, "http", hostName);
        }

        /// <summary>
        ///     Returns HTTP virtual path for the site
        /// </summary>
        /// <param name="site"></param>
        /// <returns></returns>
        public static string GetHttpVirtualPath(this Site site)
        {
            return GetVirtualPath(site, "http", "");
        }

        /// <summary>
        ///     Returns HTTPS virtual path for the site
        /// </summary>
        /// <param name="site"></param>
        /// <param name="hostName"></param>
        /// <returns></returns>
        public static string GetHttpsVirtualPath(this Site site, string hostName)
        {
            return GetVirtualPath(site, "https", hostName);
        }

        /// <summary>
        ///     Returns HTTPS virtual path for the site
        /// </summary>
        /// <param name="site"></param>
        /// <returns></returns>
        public static string GetHttpsVirtualPath(this Site site)
        {
            return GetVirtualPath(site, "https", "");
        }

        /// <summary>
        ///     Returns unique application name for given site
        /// </summary>
        /// <param name="site"></param>
        /// <param name="appName"></param>
        /// <returns></returns>
        public static string GetUniqueApplicaionName(this Site site, string appName)
        {
            appName = appName.Trim('/');

            var counter = 0;     
            var uniquePath = appName;
            while (true)
            {
                var path = uniquePath;
                var apps = site.Applications.Where(
                    a => string.Compare(a.Path.Trim('/'), path, StringComparison.InvariantCultureIgnoreCase) == 0);
                if (!apps.Any())
                {
                    break;
                }

                uniquePath = appName + "_" + (++counter).ToString(CultureInfo.InvariantCulture);
            }

            return uniquePath;
        }

        internal static string GetVirtualPath(Site site, string protocol, string hostName)
        {
            Binding binding;
            var bindings = site.Bindings.Where(b => b.Protocol == protocol);
            var enumerable = bindings as IList<Binding> ?? bindings.ToList();
            if (enumerable.Any())
            {
                binding = enumerable.Single();
            }
            else
            {
                throw new Exception(string.Format("Binding for protocol '{0}' is not defined for the website '{1}'.", protocol, site.Name));
            }

            var host = "localhost";
            if (!string.IsNullOrEmpty(hostName))
            {
                host = hostName;
            } else if (!string.IsNullOrEmpty(binding.Host))
            {
                host = binding.Host;
            }

            return binding.EndPoint.Port == 80 
                ? string.Format("{0}://{1}", protocol, host) 
                : string.Format("{0}://{1}:{2}", protocol, host, binding.EndPoint.Port);
        }
    }
}
