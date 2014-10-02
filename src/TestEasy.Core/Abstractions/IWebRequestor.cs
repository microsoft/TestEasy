namespace TestEasy.Core.Abstractions
{
    public interface IWebRequestor
    {
        bool PingUrl(string url);
    }
}
