namespace ChonkyWebTests
{
    using chonkyweb.Controllers;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;
    using StockDataLibraryTests.Mocks;
    using StockDataLibrary;
    using System;
    using ChonkyWeb.Services;

    public class NopeControllerTests
    {
        private readonly NopeController nopeController;
        private readonly Mock<ILogger<NopeController>> logger = new();
        private readonly Mock<ChonkyConfiguration> config = new MockChonkyConfiguration();
        private readonly Mock<SSEClientManager> clientManager = new();
        private readonly Mock<INopeDataService> dataService = new();
        private readonly Mock<WsSocketManager> socketManager = new();

        public NopeControllerTests()
        {
            nopeController = new NopeController(logger.Object, config.Object, clientManager.Object, dataService.Object, socketManager.Object);
        }

        [Fact]
        public async void TestCantFetchDataOlderThan8Days()
        {
            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var tooOld = now - 691200001;
            var errorMessage = "Requests for data older than 7 days are prohibited";
            var controllerResponse = await nopeController.GetNope("SPY", 0, "3/10/2021");
            Assert.Equal(errorMessage, controllerResponse.Content);
            controllerResponse = await nopeController.GetNope("SPY", tooOld);
            Assert.Equal(errorMessage, controllerResponse.Content);
        }
    }
}
