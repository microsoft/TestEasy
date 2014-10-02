using Microsoft.Web.Administration;

namespace TestEasy.WebServer
{
    /// <summary>
    ///     Wrapper that provides access to IIS metabase
    /// </summary>
    public class ServerManagerProvider : IServerManagerProvider
    {
        private ServerManager _serverManager;

        /// <summary>
        ///     Returns Microsoft.Web.Administration's ServerManager, which is an entry point to 
        ///     IIS metabase
        /// </summary>
        /// <param name="configPath"></param>
        /// <returns></returns>
        public ServerManager GetServerManager(string configPath)
        {
            _serverManager = string.IsNullOrEmpty(configPath) ? new ServerManager() : new ServerManager(configPath);

            return _serverManager;
        }

        /// <summary>
        ///     Commit metabase changes
        /// </summary>
        public void CommitChanges()
        {
            _serverManager.CommitChanges();
        }
    }
}
