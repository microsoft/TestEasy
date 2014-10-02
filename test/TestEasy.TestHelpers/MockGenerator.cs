using System;
using Moq;
using TestEasy.Core.Abstractions;

namespace TestEasy.TestHelpers
{
    public class MockGenerator
    {
        public static string Randomize(string val)
        {
            var r1 = new Random((int) DateTime.Now.Ticks/2);
            var r2 = new Random(r1.Next());
            val = val + "_" + r1.Next() + "_" + r2.Next();

            return val;
        }

        public Mock<IFileSystem> FileSystem { get; private set; }
        public Mock<IEnvironmentSystem> EnvironmentSystem { get; private set; }
        public Mock<IProcessRunner> ProcessRunner { get; private set; }

        public MockGenerator()
        {
            FileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            EnvironmentSystem = new Mock<IEnvironmentSystem>(MockBehavior.Strict);
            ProcessRunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        }
    }
}
