
namespace TestEasy.Azure
{
    internal class Dependencies
    {
        public static Subscription Subscription { get; set; }

        private static TestResourcesCollector _testResourcesCollector;
        public static TestResourcesCollector TestResourcesCollector
        {
            get { return _testResourcesCollector ?? (_testResourcesCollector = new TestResourcesCollector()); }
            set { _testResourcesCollector = value; }
        }
    }
}
