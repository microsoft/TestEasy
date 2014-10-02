using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestEasy.Azure.Helpers;
using TestEasy.Core;

namespace TestEasy.Azure.Bvt
{
    [TestClass]
    public class SqlAzureTests
    {
        /// <summary>
        /// This tesy shows E2E workflow using basic Azure Sql Server and database actions:
        ///  - create/delete/find server
        ///  - add firewall rules to a server
        ///  - create/delete/find database
        ///  - access database's Connection string        
        /// </summary>
        [TestMethod]
        public void AzureSql_E2E_Test()
        {
            // Note: !!! it is recommended though to use "using" statement since it would call Dispose even in 
            // the case of exception, which would ensure all resources cleaned up.
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {

                // Create Azure Sql server. Notice that here we specified admin user name and password, however all parameters
                // are optional. If user and password are not specified, TestEasy default ones will be used.
                var server = portal.Sql.CreateSqlServer(user: "testeasybvt", password: "MyTestBvt!13");

                // by default when we use Sql.CreateServer, current machine is added to firewall rules by TestEasy
                // however you can add your own server firewall rules
                var rule = server.CreateFirewallRule("mydummyrule", "10.0.0.1", "10.0.0.100");

                Assert.IsTrue(server.FirewallRuleExists(rule.Name));
                Assert.AreEqual("10.0.0.1", rule.StartIPAddress);
                Assert.AreEqual("10.0.0.100", rule.EndIPAddress);

                // delete now our firewall rule
                server.DeleteFirewallRule(rule.Name);

                Assert.IsFalse(server.FirewallRuleExists(rule.Name));

                // create a database on just created server
                var database = server.CreateDatabase("MyTestDatabase");

                // check if database was created for server
                Assert.AreEqual(1, server.Databases.Count());

                // just dummy check that connection string is not empty
                Assert.IsFalse(string.IsNullOrEmpty(database.ConnectionString));

                // now remove database
                server.DropDatabase(database);
                Assert.AreEqual(0, server.Databases.Count());

                // remove server
                portal.Sql.DeleteSqlServer(server.Name);

                // check if server was deleted
                Assert.IsNull(portal.Sql.GetSqlServer(server.Name));
            }
        }

        /// <summary>
        /// Shows the fastest way to create a database from scratch.
        /// Also shows how TestEasyLog can be used. Check console output for this test.
        /// </summary>
        [TestMethod]
        [TestCategory("BVT")]
        public void AzureSql_FastCreateDatabase_Test()
        {
            TestEasyLog.Instance.StartScenario("Sample for quick database creation");

            // Note: !!! it is recommended though to use "using" statement since it would call Dispose even in 
            // the case of exception, which would ensure all resources cleaned up.
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {

                TestEasyLog.Instance.StartScenario("Create database");

                // This is fastest way to create database from scratch.
                // Note: if you don't specify server name here, new server will be created for your database.
                // If you would remove server at the end it would be ok, however if each test would createa separate 
                // server for each scenario it may overload subscription capacity. To avoid that make sure you reuse 
                // servers and databases accross scenario of the same test class, unless it is needed by scenario to
                // have fresh objects. 
                // In this API default user and password would be used to create server and your machine's IP would be 
                // automatically added to firewallrules on a new server.
                var database = portal.Sql.CreateSqlDatabase("mytestdbx");

                TestEasyLog.Instance.EndScenario("Database created");

                // check if database was created for server
                Assert.AreEqual(1, database.Server.Databases.Count());

                // just dummy check that connection string is not empty
                Assert.IsFalse(string.IsNullOrEmpty(database.ConnectionString));

                TestEasyLog.Instance.StartScenario("Drop database");
                // now remove database and server
                database.Server.DropDatabase(database);

                TestEasyLog.Instance.EndScenario("Database dropped");

                // check if server was deleted
                Assert.IsFalse(database.Server.DatabaseExists(database));
            }

            TestEasyLog.Instance.EndScenario("Sample complete");
        }

        /// <summary>
        /// Shows the fastest way to create a database from scratch.
        /// Also shows how TestEasyLog can be used. Check console output for this test.
        /// </summary>
        [TestMethod]
        [TestCategory("BVT")]
        public void AzureSql_ExplicitResourceCleanup_Test()
        {
            // Create an entry point to all Azure functionality
            var portal = new AzurePortal(TestConstants.TestSubscription);

            var database = portal.Sql.CreateSqlDatabase("mytestdbx");

            // check if database was created for server
            Assert.AreEqual(1, database.Server.Databases.Count());
            Assert.IsFalse(string.IsNullOrEmpty(database.ConnectionString));

            // It is important to call this method in the end of your test to clean up all Azure resources
            // you have used explicitly or implicitly while using TestEasy API. TestEasy remembers all resources 
            // that were created and frees them. If we don't free resources subscription will reach it's limits soon,
            // and tests will be blocked.
            // Note: !!! it is recommended though to use "using" statement since it would call Dispose even in 
            // the case of exception, which would ensure all resources cleaned up.
            portal.CleanupResources();

            // check if server was deleted
            Assert.IsFalse(database.Server.DatabaseExists(database));

        }
        /// <summary>
        /// Shows how existing database can be published to another database.
        /// Note: Databases can be any, not only Azure, you can specify any connection string andchoose DB type:
        ///     - FullSql
        ///     - MySql
        ///     - SqlCe
        /// </summary>
        [TestMethod]
        [TestCategory("BVT")]
        public void AzureSql_PublishDatabase_Test()
        {
            // Note: !!! it is recommended though to use "using" statement since it would call Dispose even in 
            // the case of exception, which would ensure all resources cleaned up.
            using (var portal = new AzurePortal(TestConstants.TestSubscription))
            {

                // create two databases from scratch (they are empty but for feature demonstration its ok)
                var databaseSource = portal.Sql.CreateSqlDatabase("mytestdbxs");
                var server = databaseSource.Server;
                var databaseDestination = portal.Sql.CreateSqlDatabase("mytestdbxd", server);

                // now use WebDeploy to publish source database to destination including data(!).
                // Note: that calling publish database can be called sequentially and next time would bring only 
                // database updates.
                portal.Sql.PublishDatabase(
                    WebDeployDatabaseType.FullSql,
                    databaseSource.ConnectionString,
                    WebDeployDatabaseType.FullSql,
                    databaseDestination.ConnectionString,
                    includeData: true);

                // now remove database and server
                server.DropDatabase(databaseSource);
                server.DropDatabase(databaseDestination);
                portal.Sql.DeleteSqlServer(server.Name);

                // check if server was deleted
                Assert.IsNull(portal.Sql.GetSqlServer(server.Name));
            }
        }

    }
}
