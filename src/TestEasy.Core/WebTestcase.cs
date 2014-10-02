using TestEasy.Core.Configuration;

namespace TestEasy.Core
{
    /// <summary>
    ///     Optional base class for all testcase using TestEasy
    /// </summary>
    public class WebTestcase
    {
        protected TestEasyConfig TestEasyConfig
        {
            get { return TestEasyConfig.Instance; }
        }
    }
}
