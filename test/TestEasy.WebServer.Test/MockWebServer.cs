namespace TestEasy.WebServer.Test
{
    public class MockWebServer : WebServer
    {
        public MockWebServer()
            :this(null, null)
        {            
        }

        public MockWebServer(WebServerSettings settings, Dependencies dependencies)
        {
            _type = "MockWebServer";
            if (dependencies != null)
            {
                _fileSystem = dependencies.FileSystem;
                _environmentSystem = dependencies.EnvironmentSystem;
                _processRunner = dependencies.ProcessRunner;
                _serverManagerProvider = dependencies.ServerManagerProvider;
            }
        }

        public override WebApplicationInfo CreateWebApplication(string webAppName, bool forceUniqueName = true)
        {
            return null;
        }

        public override void DeleteWebApplication(string webAppName, bool deletePhysicalFolder = true)
        {
            
        }

        private WebApplicationInfo _webApplicationInfo;
        public WebApplicationInfo WebApplicationInfo
        {
            get { return _webApplicationInfo; }
            set { _webApplicationInfo = value; }
        }
        public override WebApplicationInfo GetWebApplicationInfo(string webAppName)
        {
            return WebApplicationInfo;
        }
    }
}
