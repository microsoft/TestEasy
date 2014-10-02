using System.Collections.Generic;

namespace TestEasy.Core
{
    /// <summary>
    ///     Basic log to be used in TestEasy assemblies to trace runtime information
    /// </summary>
    public class TestEasyLog
    {
        private static TestEasyLog _instance;

        /// <summary>
        ///     static log singletin instance
        /// </summary>
        public static TestEasyLog Instance
        {
            get { return _instance ?? (_instance = new TestEasyLog()); }
        }

        private List<ITestEasyLogProvider> _logProviders;

        /// <summary>
        ///     List of log providers for different log formats
        /// </summary>
        public List<ITestEasyLogProvider> LogProviders
        {
            get { return _logProviders ?? (_logProviders = new List<ITestEasyLogProvider>()); }
        }

        internal TestEasyLog()
            : this(new TestEasyConsoleLogProvider())
        {            
        }

        internal TestEasyLog(ITestEasyLogProvider logProvider)
        {
            if (logProvider != null)
            {
                LogProviders.Add(logProvider);
            }
        }

        /// <summary>
        ///     Start log scenario section
        /// </summary>
        /// <param name="scenarioName"></param>
        public void StartScenario(string scenarioName)
        {
            LogProviders.ForEach(p => p.StartScenario(scenarioName));
        }

        /// <summary>
        ///     Complete log scenario section
        /// </summary>
        /// <param name="message"></param>
        public void EndScenario(string message)
        {
            LogProviders.ForEach(p => p.EndScenario(message));
        }

        /// <summary>
        ///     Log informational message
        /// </summary>
        /// <param name="message"></param>
        public void Info(string message)
        {
            LogProviders.ForEach(p => p.Info(message));
        }

        /// <summary>
        ///     Log success message
        /// </summary>
        /// <param name="message"></param>
        public void Success(string message)
        {
            LogProviders.ForEach(p => p.Success(message));
        }

        /// <summary>
        ///     Log failure message
        /// </summary>
        /// <param name="message"></param>
        public void Failure(string message)
        {
            LogProviders.ForEach(p => p.Failure(message));
        }

        /// <summary>
        ///     Log warning message
        /// </summary>
        /// <param name="message"></param>
        public void Warning(string message)
        {
            LogProviders.ForEach(p => p.Warning(message));
        }

        /// <summary>
        ///     Store log in all providers
        /// </summary>
        public void StoreLog()
        {
            LogProviders.ForEach(p => p.StoreLog());
        }
    }
}
