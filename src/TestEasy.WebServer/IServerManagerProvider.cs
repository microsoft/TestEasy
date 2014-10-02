using Microsoft.Web.Administration;

namespace TestEasy.WebServer
{
    public interface IServerManagerProvider
    {
        ServerManager GetServerManager(string configPath);
        void CommitChanges();
    }
}
