using System;

namespace TestEasy.Azure.Sql
{
    /// <summary>
    ///     Azure sql database model
    /// </summary>
    public class SqlAzureDatabase
    {
        public static string BuildConnectionString(string serverName, string databaseName, string user, string password)
        {
            return string.Format(AzureServiceConstants.AzureSqlConnectionString, serverName, databaseName, user, password, Dependencies.Subscription.DatabaseDomain);
        }

        public SqlAzureServer Server { get; set; }
        public string Name { get; set; }
        public SqlAzureDatabaseMaxSize MaxSize { get; set; }

        public string ConnectionString
        {
            get { return BuildConnectionString(Server.Name, Name, Server.User, Server.Password); }
        }

        internal SqlAzureDatabase(SqlAzureServer server, string databaseName)
            : this(server, databaseName, SqlAzureDatabaseMaxSize.Gb1)
        {
        }

        internal SqlAzureDatabase(
            SqlAzureServer server,
            string databaseName, 
            SqlAzureDatabaseMaxSize maxSize)
        {
            if (server == null)
            {
                throw new ArgumentNullException("server");
            }

            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentNullException("databaseName");
            }

            Name = databaseName;
            Server = server;
            MaxSize = maxSize;
        }
        
        internal string GetCreateSqlStatement()
        {
            var editingOptions = "";

            if (MaxSize != SqlAzureDatabaseMaxSize.Gb1)
            {
                editingOptions += "MAXSIZE = " + ((int)MaxSize) + "GB";
            }

            if (string.IsNullOrEmpty(editingOptions))
            {
                return string.Format(@"CREATE DATABASE {0}", Name); 
            }

            return string.Format(@"CREATE DATABASE {0} ( {1} )", Name, editingOptions);
        }

        internal string GetAlterSqlStatement()
        {
            var editingOptions = "";

            if (MaxSize != SqlAzureDatabaseMaxSize.Gb1)
            {
                editingOptions += "MODIFY ( MAXSIZE = " + ((int)MaxSize) + "GB )";
            }

            return string.Format(@"ALTER DATABASE  {0} {1}", Name, editingOptions);
        }
        
        internal string GetDropSqlStatement()
        {
            return string.Format(@"DROP DATABASE {0}", Name);
        }
    }
}
