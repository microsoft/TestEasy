namespace TestEasy.Core
{
    public interface ITestEasyLogProvider
    {
        void StartScenario(string scenarioName);
        void EndScenario(string message);
        void Info(string message);
        void Success(string message);
        void Failure(string message);
        void Warning(string message);
        void StoreLog();
    }
}
