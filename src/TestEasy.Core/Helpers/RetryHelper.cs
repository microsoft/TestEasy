using System;
using System.Threading;

namespace TestEasy.Core.Helpers
{
    /// <summary>
    ///     API helps to retry some Func or Actions that may throw some exceptions
    /// </summary>
    public static class RetryHelper
    {
        public static void Retry(Action action, int retries)
        {
            Retry(action, retries, TimeSpan.FromSeconds(0.0));
        }

        public static void Retry(Action action, int retries, TimeSpan delay)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            var num = 0;
            do
            {
                num++;
                try
                {
                    action();
                    break;
                }
                catch (Exception ex)
                {
                    TestEasyLog.Instance.Info(string.Format("Attempt {0} of {1} failed with exception: {2}", num, retries + 1, ex.Message));
                    if (num > retries)
                    {
                        TestEasyLog.Instance.Info(string.Format("Action failed after '{0}' attempts.", retries));
                        throw;
                    }

                    Thread.Sleep(delay);
                }
            }
            while (num <= retries);
        }

        public static bool RetryUntil(Func<bool> predicate, int retries)
        {
            return RetryUntil(predicate, retries, TimeSpan.FromSeconds(0.0));
        }

        public static bool RetryUntil(Func<bool> predicate, int retries, TimeSpan delay)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }

            var num = 0;
            bool flag;
            do
            {
                flag = predicate();

                if (flag) break;
                
                num++;
                TestEasyLog.Instance.Info(string.Format("Attempt {0} of {1} failed.", num, retries + 1));

                Thread.Sleep(delay);

            }
            while (num <= retries);

            if (num > retries)
            {
                TestEasyLog.Instance.Info(string.Format("Action failed after '{0}' attempts.", retries));
            }

            return flag;
        }
    }
}
