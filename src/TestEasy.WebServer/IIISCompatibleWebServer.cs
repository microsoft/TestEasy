using Microsoft.Web.Administration;

namespace TestEasy.WebServer
{
    public interface IIisCompatibleWebServer
    {
        ServerManager ServerManager { get; }
    }
}
