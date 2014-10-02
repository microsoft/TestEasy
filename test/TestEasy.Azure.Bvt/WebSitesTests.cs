using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Management.WebSites.Models;
using TestEasy.Core;

namespace TestEasy.Azure.Bvt
{
    [TestClass]
    [DeploymentItem("SampleWebSites")]
    public class WebSitesTests
    {
        [TestInitialize]
        [TestCleanup]
        public void Cleanup()
        {
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                if (portal.WebSites.WebSiteExists(TestConstants.WebSiteName))
                {
                    portal.WebSites.DeleteWebSite(TestConstants.WebSiteName);
                }
            }
        }

        [TestMethod]
        public void AzureWebSites_E2E_Test()
        {
            // Note: !!! it is recommended though to use "using" statement since it would call Dispose even in 
            // the case of exception, which would ensure all resources cleaned up.
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                // Create Azure WebSite
                var site = portal.WebSites.CreateWebSite(TestConstants.WebSiteName);
                Assert.IsTrue(portal.WebSites.WebSiteExists(TestConstants.WebSiteName));

                // Deploy website content
                portal.WebSites.PublishWebSite(Path.GetFullPath("TestEasyWebSite"), TestConstants.WebSiteName);

                // Ping website
                Assert.IsTrue(TestEasyHelpers.Web.PingUrl(site.GetUrl() + "/default.html"));
            }
        }

        [TestMethod]
        public void AzureWebSites_AddConnectionStrings_Test()
        {
            // Note: !!! it is recommended though to use "using" statement since it would call Dispose even in 
            // the case of exception, which would ensure all resources cleaned up.
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                // Create Azure WebSite
                portal.WebSites.CreateWebSite(TestConstants.WebSiteName);
                Assert.IsTrue(portal.WebSites.WebSiteExists(TestConstants.WebSiteName));

                // When modifiying the site config, start by getting the existing config.
                // Otherwise, you might delete important settings, such as default documents.
                var config = portal.WebSites.GetWebSiteConfig(TestConstants.WebSiteName);
                var updateParams = portal.WebSites.CreateWebSiteUpdateParameters(config);
                updateParams.ConnectionStrings.Add(new WebSiteUpdateConfigurationParameters.ConnectionStringInfo { 
                    Name = "MyConnectionString", 
                    ConnectionString = "Initial Catalog=test",
                    Type = "SQLAzure",
                });

                // Add a new connection string
                portal.WebSites.UpdateWebSiteConfig(TestConstants.WebSiteName, updateParams);

                // verify the connection string was added
                config = portal.WebSites.GetWebSiteConfig(TestConstants.WebSiteName);
                Assert.AreEqual(1, config.ConnectionStrings.Count);
            }
        }

        /// <summary>
        /// Shows the fastest way to create a website with database from scratch
        /// </summary>
        [TestMethod]
        public void AzureSql_FastCreateWebSiteWithDatabase_Test()
        {
            // Note: !!! it is recommended though to use "using" statement since it would call Dispose even in 
            // the case of exception, which would ensure all resources cleaned up.
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                // This is fastest way to create database from scratch.
                // Note: if you don't specify server name here, new server will be created for your database.
                // If you would remove server at the end it would be ok, however if each test would createa separate 
                // server for each scenario it may overload subscription capacity. To avoid that make sure you reuse 
                // servers and databases accross scenario of the same test class, unless it is needed by scenario to
                // have fresh objects. 
                // In this API default user and password would be used to create server and your machine's IP would be 
                // automatically added to firewallrules on a new server.
                var database = portal.Sql.CreateSqlDatabase("mytestdbx");

                // check if database was created for server
                Assert.AreEqual(1, database.Server.Databases.Count());

                // just dummy check that connection string is not empty
                Assert.IsFalse(string.IsNullOrEmpty(database.ConnectionString));

                var site = portal.WebSites.CreateWebSite(
                    connectionStringInfo: new WebSiteUpdateConfigurationParameters.ConnectionStringInfo
                    {
                        Name = "DefaultConnectionString",
                        ConnectionString = database.ConnectionString,
                        Type = "SQLAzure",
                    });

                Assert.IsTrue(TestEasyHelpers.Web.PingUrl(site.GetUrl()));

                // now remove database and server
                database.Server.DropDatabase(database);
                portal.Sql.DeleteSqlServer(database.Server.Name);

                // check if server was deleted
                Assert.IsNull(portal.Sql.GetSqlServer(database.Server.Name));
            }
        }

        [TestMethod]
        public void AzureWebSites_E2E_FTP_Test()
        {
            // Note: !!! it is recommended though to use "using" statement since it would call Dispose even in 
            // the case of exception, which would ensure all resources cleaned up.
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {
                // Create Azure WebSite
                var site = portal.WebSites.CreateWebSite(TestConstants.WebSiteName);
                Assert.IsTrue(portal.WebSites.WebSiteExists(TestConstants.WebSiteName));

                // Deploy website content
                portal.WebSites.PublishWebSite(Path.GetFullPath("TestEasyWebSite"), TestConstants.WebSiteName, PublishMethod.Ftp, siteRootRelativePath: "test/musicstore");

                // Ping website
                Assert.IsTrue(TestEasyHelpers.Web.PingUrl(site.GetUrl() + "/test/musicstore/default.html"));
            }
        }
    }
}
