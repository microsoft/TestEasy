using System;

namespace TestEasy.Core
{
    /// <summary>
    ///     Console log provider, outputs log messages to console
    /// </summary>
    public class TestEasyConsoleLogProvider : ITestEasyLogProvider
    {
        private string _indent;
        private readonly int _indentStep;
 
        public TestEasyConsoleLogProvider()
        {
            _indent = "";
            _indentStep = 4;
        }

        public void StartScenario(string scenarioName)
        {
            AppendLog("* " + scenarioName);

            _indent = _indent + "    ";
        }

        public void EndScenario(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = "Scenario complete";

            }
            AppendLog(message);

            if (string.IsNullOrEmpty(_indent)) return;

            var newLength = _indent.Length - _indentStep;

            _indent = newLength - 1 > 0 ? _indent.Substring(newLength - 1) : "";
        }

        public void Info(string message)
        {
            AppendLog(message);
        }

        public void Success(string message)
        {
            AppendLog("[Success] " + message);
        }

        public void Failure(string message)
        {
            AppendLog("[Failure] " + message);
        }
        
        public void Warning(string message)
        {
            AppendLog("[Warning] " + message);
        }

        public void StoreLog()
        {
            // for console do nothing
        }

        private void AppendLog(string message)
        {
            Console.WriteLine("{0} {1}{2}", DateTime.Now, _indent, message);
        }
    }
}
