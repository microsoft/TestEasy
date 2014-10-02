using Xunit;
using Moq;
using TestEasy.Core.Abstractions;
using TestEasy.Core.Helpers;

namespace TestEasy.Core.Test
{
    public class WebHelperFacts
    {
        [Fact]
        public void PingUrlFact()
        {
            // Arrange
            var mockWebRequestor = new Mock<IWebRequestor>(MockBehavior.Strict);
            mockWebRequestor.Setup(m => m.PingUrl("myurl")).Returns(true);

            // Act     
            var helper = new WebHelper(mockWebRequestor.Object);
            var result = helper.PingUrl("myurl");

            Assert.True(result);
        }
    }
}
