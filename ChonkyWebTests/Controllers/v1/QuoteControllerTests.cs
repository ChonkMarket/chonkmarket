namespace ChonkyWebTests.Controllers.v1
{
    using AutoMapper;
    using ChonkyWeb.Controllers.v1;
    using ChonkyWeb.Models.V1ApiModels;
    using ChonkyWeb.Modelsl.V1ApiModels;
    using ChonkyWeb.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Moq;
    using StockDataLibrary;
    using StockDataLibrary.Db;
    using StockDataLibraryTests.Mocks;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;

    public class QuoteControllerTests
    {
        private readonly MockChonkyConfiguration mockChonkyConfiguration = new();
        private readonly Mock<ILogger<QuoteController>> loggerMock = new();
        private readonly Mock<INopeDataService> nopeDataServiceMock = new();
        private readonly Mock<SSEClientManager> clientManagerMock = new();
        private readonly Mock<TestDataSenderFactory> testDataSenderFactoryMock = new(new Mock<ICass>().Object, new Mock<IMapper>().Object);
        private readonly QuoteController quoteController;

        public QuoteControllerTests()
        {
            quoteController = new QuoteController(loggerMock.Object, mockChonkyConfiguration.Object, clientManagerMock.Object, nopeDataServiceMock.Object, testDataSenderFactoryMock.Object);
        }

        [Fact]
        public async Task ReturnsErrorForInvalidTicker()
        {
            var resp = await quoteController.GetQuotes("SASD") as JsonResult;
            Assert.Equal(StatusCodes.Status404NotFound, resp.StatusCode);
            var data = resp.Value as V1Error;
            AssertFail(data, "Invalid Ticker: SASD");
        }

        [Fact]
        public async Task ReturnsErrorForMarketClosedDates()
        {
            var resp = await quoteController.GetQuotes("SPY", date: "4/4/2021") as JsonResult;
            var data = resp.Value as V1Error;
            Assert.Equal(StatusCodes.Status404NotFound, resp.StatusCode);
            AssertFail(data, "Market Closed on 4/4/2021");
        }

        [Fact]
        public async Task ReturnsErrorIfNoDateOrQuotetime()
        {
            var resp = await quoteController.GetQuotes("SPY") as JsonResult;
            var data = resp.Value as V1Error;
            Assert.Equal(StatusCodes.Status400BadRequest, resp.StatusCode);
            AssertFail(data, "Please specify either a date or a quotetime.");
        }

        [Fact]
        public void GetsTickers()
        {
            var resp = quoteController.GetTickers();
            Assert.True(resp.Success);
            Assert.Equal("Array<string>", resp.DataType);
        }

        // this test is stupid brittle
        // mocked to death
        // doesn't reflect underlying database changes
        //
        [Fact]
        public async Task ReturnsValidData()
        {
            var json = $"{{\"dataType\":\"quotes\",\"data\":{{\"info\":{{\"symbol\":\"spy\"}},\"quotes\":[{{\"mark\":400.2,\"quoteTime\":1234125,\"totalVolume\":1245542,\"totalPutOptionDelta\":2342401,\"totalCallOptionDelta\":4023404,\"nope\":11.21,\"localPutOptionDelta\":1245341,\"localCallOptionDelta\":123124,\"localVolume\":0}}]}},\"success\":true}}";
            var date = "4/8/2021";
            var hours = TradingHours.GetMarketHours(date);
            nopeDataServiceMock.Setup(x => x.GetJsonAsync("SPY", 0, date, hours)).ReturnsAsync(json);
            var resp = await quoteController.GetQuotes("SPY", date: date) as ContentResult;
            Assert.Equal(StatusCodes.Status200OK, resp.StatusCode);
            var data = resp.Content;
            Assert.Equal(json, data);
        }

        private static void AssertFail(V1Error resp, string message)
        {
            Assert.False(resp.Success);
            Assert.Equal(message, resp.Message);
        }
    }
}
