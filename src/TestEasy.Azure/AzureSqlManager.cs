using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.WindowsAzure.Management.Sql;
using Microsoft.WindowsAzure.Management.Sql.Models;
using TestEasy.Azure.Helpers;
using TestEasy.Azure.Sql;
using TestEasy.Core;

namespace TestEasy.Azure
{
    /// <summary>
    ///     Contains helper methods for managing Azure Sql servers and databases
    /// </summary>
    public class AzureSqlManager
    {
        /// <summary>
        ///     Rest API client
        /// </summary>
        public ISqlManagementClient SqlManagementClient
        {
            get;
            private set;
        }

        internal AzureSqlManager()
        {            
            SqlManagementClient = new SqlManagementClient(Dependencies.Subscription.Credentials,
                new Uri(Dependencies.Subscription.CoreEndpointUrl));
        }

        /// <summary>
        ///     Create sql server
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="location"></param>
        /// <param name="reuseExistingIfPossible"></param>
        /// <returns></returns>
        public SqlAzureServer CreateSqlServer(string user = AzureServiceConstants.SqlServerAdminLogin,
            string password = AzureServiceConstants.SqlServerAdminPassword,
            string location = null,
            bool reuseExistingIfPossible = true)
        {
            if(string.IsNullOrEmpty(location))
            {
                location = Dependencies.Subscription.DefaultLocation;
            }

            SqlAzureServer server = null;
            var ip = GetInterNetworkIp();
            var pip = GetPublicIP();

            if (reuseExistingIfPossible)
            {
                server = FindServerToReuse(user, password);
            }

            if (server == null)
            {
                TestEasyLog.Instance.Info(string.Format("Creating azure sql server with login '{0}'", user));

                var newServerName = SqlManagementClient.Servers.CreateAsync(new ServerCreateParameters
                {
                    AdministratorPassword = password,
                    AdministratorUserName = user,
                    Location = location,
                }, new CancellationToken()).Result.ServerName;

                server = new SqlAzureServer(newServerName, user, password);
            }
            Dependencies.TestResourcesCollector.Remember(AzureResourceType.SqlServer, server.Name, server);

            server.CreateFirewallRule("azureapps", "0.0.0.0", "0.0.0.0");
            server.CreateFirewallRule("workaround", "0.0.0.0", "255.255.255.255");
            server.CreateFirewallRule(Environment.MachineName + "_ip", ip, ip);
            server.CreateFirewallRule(Environment.MachineName + "_pip", pip, pip);


            return server;
        }

        private SqlAzureServer FindServerToReuse(string user, string password)
        {
            SqlAzureServer server = null;

            try
            {
                var servers = GetSqlServersDetails();
                foreach (var s in servers)
                {
                    if (string.Compare(
                        s.AdministratorUserName, user,
                        StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        var tempServer = new SqlAzureServer(s.Name, user, password);
                        
                        if (!tempServer.CanHaveMoreDatabases) continue;

                        server = tempServer;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                TestEasyLog.Instance.Warning(string.Format("Could not reuse SqlServers, there was an exception: '{0}'", e.Message));
            }

            return server;
        }

        /// <summary>
        ///     Delete sql server
        /// </summary>
        /// <param name="serverName"></param>
        public void DeleteSqlServer(string serverName)
        {
            TestEasyLog.Instance.Info(string.Format("Deleting azure sql server '{0}'", serverName));
            SqlManagementClient.Servers.DeleteAsync(serverName, new CancellationToken()).Wait();
            Dependencies.TestResourcesCollector.Forget(AzureResourceType.SqlServer, serverName);
        }

        /// <summary>
        ///     List all sql servers
        /// </summary>
        /// <returns></returns>
        public IList<Server> GetSqlServersDetails()
        {
            TestEasyLog.Instance.Info("Getting Azure Sql servers");

            var result = SqlManagementClient.Servers.ListAsync(new CancellationToken()).Result;

            return result.Servers;
        }

        /// <summary>
        ///     Get sql server information
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public SqlAzureServer GetSqlServer(
            string serverName,
            string user = AzureServiceConstants.SqlServerAdminLogin,
            string password = AzureServiceConstants.SqlServerAdminPassword)
        {
            TestEasyLog.Instance.Info(string.Format("Getting Azure Sql server '{0}'", serverName));

            var server = GetSqlServersDetails().FirstOrDefault(s => s.Name == serverName);
            if (server == null)
            {
                return null;
            }

            return new SqlAzureServer(server.Name, server.AdministratorUserName, password);
        }

        /// <summary>
        ///     Create a database on sql server
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="serverName"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        public SqlAzureDatabase CreateSqlDatabase(
            string databaseName,
            string serverName = "", 
            string user = AzureServiceConstants.SqlServerAdminLogin,
            string password = AzureServiceConstants.SqlServerAdminPassword,
            SqlAzureDatabaseMaxSize maxSize = SqlAzureDatabaseMaxSize.Gb1)
        {
            var server = string.IsNullOrEmpty(serverName) ? CreateSqlServer(user, password) : GetSqlServer(serverName, user, password);

            return CreateSqlDatabase(databaseName, server, maxSize);
        }

        /// <summary>
        ///     Create a database on sql server
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="server"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        public SqlAzureDatabase CreateSqlDatabase(
            string databaseName,
            SqlAzureServer server,
            SqlAzureDatabaseMaxSize maxSize = SqlAzureDatabaseMaxSize.Gb1)
        {

            var existingDatabasesOnServer = SqlManagementClient.Databases.ListAsync(server.Name, new CancellationToken()).Result.Databases;
            
            if(existingDatabasesOnServer.Any(d => d.Name == databaseName))
            {
                SqlManagementClient.Databases.DeleteAsync(server.Name, databaseName, new CancellationToken()).Wait();
            }

            return server.CreateDatabase(databaseName, maxSize);
        }

        /// <summary>
        ///     Returns public IP
        /// </summary>
        /// <returns></returns>
        public string GetPublicIP()
        {
            var client = new WebClient();
            return client.DownloadString("http://ipecho.net/plain"); //"http://bot.whatismyipaddress.com");
        }

        /// <summary>
        ///     Returns internal network IP
        /// </summary>
        /// <returns></returns>
        public string GetInterNetworkIp()
        {
            var ip = Dns.GetHostEntry(Dns.GetHostName());
            return ip.AddressList.FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetwork).ToString();
        }

        /// <summary>
        ///     Publish database to the cloud
        /// </summary>
        /// <param name="sourceDbType"></param>
        /// <param name="sourceConnectionString"></param>
        /// <param name="destinationDbType"></param>
        /// <param name="destinationConnectionString"></param>
        /// <param name="includeData"></param>
        public void PublishDatabase(WebDeployDatabaseType sourceDbType,
            string sourceConnectionString,
            WebDeployDatabaseType destinationDbType,
            string destinationConnectionString,
            bool includeData = false)
        {
            TestEasyLog.Instance.Info(string.Format("Publishing database '{0}' to '{1}'", sourceConnectionString, destinationConnectionString));

            WebDeployHelper.DeployDatabase(sourceDbType, sourceConnectionString, destinationDbType, destinationConnectionString, includeData);
        }
    }
}