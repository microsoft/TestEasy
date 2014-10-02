using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using TestEasy.Azure.Helpers;
using WAML = Microsoft.WindowsAzure.Management;
using System.Threading;
using TestEasy.Core;

namespace TestEasy.Azure.Sql
{
    /// <summary>
    ///     Azure sql server model
    /// </summary>
    public class SqlAzureServer
    {
        public string Name { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        private SqlAzureDatabase MasterDatabase { get; set; }
        private WAML.Sql.SqlManagementClient wamlClient;

        internal SqlAzureServer(string serverName)
            : this(serverName, AzureServiceConstants.SqlServerAdminLogin, AzureServiceConstants.SqlServerAdminPassword)
        {
        }

        internal SqlAzureServer(
            string serverName, 
            string user,
            string password)
        {
            if (string.IsNullOrEmpty(serverName))
            {
                throw new ArgumentNullException("serverName");
            }

            if (string.IsNullOrEmpty(user))
            {
                throw new ArgumentNullException("user");
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException("password");
            }

            Name = serverName;
            User = user;
            Password = password;

            MasterDatabase = new SqlAzureDatabase(this, "master");

            wamlClient = new WAML.Sql.SqlManagementClient(Dependencies.Subscription.Credentials,
                new Uri(Dependencies.Subscription.CoreEndpointUrl));
        }

        public bool CanHaveMoreDatabases
        {
            get
            {
                return Databases.Count() <= 148;
            }
        }

        public IEnumerable<SqlAzureDatabase> Databases
        {
            get { return GetDatabases(); }
        }

        public IEnumerable<WAML.Sql.Models.FirewallRule> FirewallRules
        {
            get { return GetFirewallRules(); }
        }

        public void ResetPassword(string newPassword)
        {
            TestEasyLog.Instance.Info(string.Format("Resetting password for azure sql server '{0}'", this.Name));

            wamlClient.Servers.ChangeAdministratorPasswordAsync(this.Name, new WAML.Sql.Models.ServerChangeAdministratorPasswordParameters { 
                 NewPassword = newPassword,
            }, new CancellationToken()).Wait();

            this.Password = newPassword;
        }

        public SqlAzureDatabase CreateDatabase(string databaseName, SqlAzureDatabaseMaxSize size = SqlAzureDatabaseMaxSize.Gb1)
        {
            if (DatabaseExists(databaseName))
            {
                throw new Exception(string.Format("Database '{0}' already exists on Sql Azure Server '{1}'.", databaseName, Name));
            }

            var database = new SqlAzureDatabase(this, databaseName, size);
            SqlHelper.ExecuteQuery(MasterDatabase.ConnectionString, database.GetCreateSqlStatement());

            Dependencies.TestResourcesCollector.Remember(AzureResourceType.SqlDatabase, database.Name, database);

            return database;
        }

        public void DropDatabase(string databaseName)
        {
            DropDatabase(new SqlAzureDatabase(this, databaseName));
        }

        public void DropDatabase(SqlAzureDatabase database)
        {
            if (!DatabaseExists(database.Name)) return;

            SqlHelper.ExecuteQuery(MasterDatabase.ConnectionString, database.GetDropSqlStatement());

            Dependencies.TestResourcesCollector.Forget(AzureResourceType.SqlDatabase, database.Name);
        }

        public void AlterDatabase(string databaseName, SqlAzureDatabaseMaxSize newSize)
        {
            AlterDatabase(new SqlAzureDatabase(this, databaseName), newSize);
        }

        public void AlterDatabase(SqlAzureDatabase database, SqlAzureDatabaseMaxSize newSize)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");    
            }

            if (!DatabaseExists(database.Name))
            {
                throw new Exception(string.Format("Database '{0}' does not exist on Sql Azure Server '{1}'.", database.Name, Name));
            }

            SqlHelper.ExecuteQuery(MasterDatabase.ConnectionString, database.GetAlterSqlStatement());
        }

        public IEnumerable<SqlAzureDatabase> GetDatabases()
        {
            var table = SqlHelper.ExecuteSelectQuery(MasterDatabase.ConnectionString, "SELECT * FROM sys.databases");

            foreach (DataRow row in table.Rows)
            {
                var databaseName = row["name"].ToString();
                if (databaseName.Equals("master", StringComparison.InvariantCultureIgnoreCase)) continue;

                var database = new SqlAzureDatabase(this, databaseName);
                var obj = SqlHelper.ExecuteFunction(database.ConnectionString,
                                                    string.Format("SELECT DATABASEPROPERTYEX('{0}', 'MaxSizeInBytes')", databaseName));

                database.MaxSize = (SqlAzureDatabaseMaxSize)(((Int64)obj)/1024/1024/1024);

                yield return database;
            }
        }

        public bool DatabaseExists(SqlAzureDatabase database)
        {
            return DatabaseExists(database.Name);
        }

        public bool DatabaseExists(string databaseName)
        {
            return Databases.Any(d => d.Name.Equals(databaseName, StringComparison.InvariantCultureIgnoreCase));
        }

        public WAML.Sql.Models.FirewallRule CreateFirewallRule(string ruleName, string startIp, string endIp)
        {

            WAML.Sql.Models.FirewallRule result;

            if(wamlClient.FirewallRules.ListAsync(this.Name, new CancellationToken()).Result
                .FirewallRules.Any(fr => fr.Name.Equals(ruleName, StringComparison.OrdinalIgnoreCase)))
            {
                TestEasyLog.Instance.Info(string.Format("Updating existing firewall rule '{1}' for azure sql server '{0}'", this.Name, ruleName));

                result = wamlClient.FirewallRules.UpdateAsync(this.Name, ruleName, new WAML.Sql.Models.FirewallRuleUpdateParameters
                {
                    Name = ruleName,
                    StartIPAddress = startIp,
                    EndIPAddress = endIp
                }, new CancellationToken()).Result.FirewallRule;
            }
            else
            {
                TestEasyLog.Instance.Info(string.Format("Creating firewall rule '{1}' for azure sql server '{0}'", this.Name, ruleName));

                result = wamlClient.FirewallRules.CreateAsync(this.Name, new WAML.Sql.Models.FirewallRuleCreateParameters
                {
                    Name = ruleName,
                    StartIPAddress = startIp,
                    EndIPAddress = endIp
                }, new CancellationToken()).Result.FirewallRule;
            }

            return result;
        }

        // TODO:
        public WAML.Sql.Models.FirewallRule CreateFirewallRuleAutoDetect(string ruleName)
        {
            throw new NotImplementedException();
            //var result = wamlClient.FirewallRules.CreateAsync(this.Name, new WAML.Sql.Models.FirewallRuleCreateParameters
            //{
            //    Name = ruleName,
            //    StartIPAddress = "",
            //    EndIPAddress = "",
            //}, new CancellationToken()).Result.FirewallRule;

            //return new SqlAzureServerFirewallRule { Name = result.Name, StartIpAddress = result.StartIPAddress, EndIpAddress = result.EndIPAddress };
        }

        public IList<WAML.Sql.Models.FirewallRule> GetFirewallRules()
        {
            TestEasyLog.Instance.Info(string.Format("Getting firewall rules for azure sql server '{0}'", this.Name));

            var result = wamlClient.FirewallRules.ListAsync(this.Name, new CancellationToken()).Result;
            
            return result.FirewallRules;
        }

        public WAML.Sql.Models.FirewallRule GetFirewallRule(string ruleName)
        {
            return GetFirewallRules().Single(r => r.Name.Equals(ruleName, StringComparison.InvariantCultureIgnoreCase));
        }

        public void DeleteFirewallRule(string ruleName)
        {
            TestEasyLog.Instance.Info(string.Format("Deleting firewall rule '{1}' for azure sql server '{0}'", this.Name, ruleName));
            wamlClient.FirewallRules.DeleteAsync(this.Name, ruleName, new CancellationToken()).Wait();
        }

        public bool FirewallRuleExists(string ruleName)
        {
            return FirewallRules.Any(r => r.Name.Equals(ruleName, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
