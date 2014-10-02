namespace TestEasy.Azure
{
    internal static class AzureServiceConstants
    {
        public const string DefaultAffinityGroup = "TestEasyAffinityGroup";
        public const string DefaultLabel = "testeasyautomation";
        public const string DefaultLocation = "West US";
        public const string DefaultWebSpace = "westuswebspace";
        public const string DefaultStorageServiceName = "testeasystorageservice";
        public const string DefaultContainer = "testeasycontainer";
        public const string DefaultHostingService = "testeasyhostedservice";
        public const string DefaultDeploymentName = "testeasydeployment";
        public const string DefaultWebSiteName = "testeasywebsite";
        public const string DefaultVmName = "testeasyvm";
        public const string DefaultSqlServerName = "testeasysqlserver";

        public const string AzureCoreEndPointUrl = "https://management.core.windows.net";

        public const string SqlServerAdminLogin = "testeasysa";
        public const string SqlServerAdminPassword = "TestEasyRocks!14";

        public const string AzureSqlConnectionString =
            "Server=tcp:{0}.{4},1433;Database={1};User ID={2}@{0};Password={3};Trusted_Connection=False;Encrypt=True;Connection Timeout=30;";
        public const string AzureSqlDefaultDomain = "database.windows.net";

        public const string ClearDbServiceName = "ClearDBDatabase";
        public const string AzureWebSitesDomainName = ".azurewebsites.net";
        public const string AzureWebDeployUrl = @"https://{0}/msdeploy.axd?site={1}";
    }
}