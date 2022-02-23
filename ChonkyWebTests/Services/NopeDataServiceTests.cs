namespace ChonkyWebTests.Services
{
    using AutoMapper;
    using ChonkyWeb;
    using ChonkyWeb.Models.V1ApiModels;
    using ChonkyWeb.Modelsl.V1ApiModels;
    using ChonkyWeb.Services;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore.Internal;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Moq;
    using StockDataLibrary;
    using StockDataLibrary.Db;
    using StockDataLibrary.Models;
    using StockDataLibraryTests.Mocks;
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading;
    using Xunit;

    public class NopeDataServiceTests
    {
        private static readonly MarketHours todayHours = TradingHours.GetMarketHours(DateTime.Now);

        private readonly NopeDataService dataService;
        private readonly Mock<ILogger<NopeDataService>> logger = new();
        private static readonly Mock<ICass> mock = new();
        private readonly Mock<ICass> cass = mock;
        private readonly Mock<IDistributedCache> redisCache = new();
        private readonly Mock<IMemoryCache> memoryCache = new();
        private readonly Mock<IWebHostEnvironment> environment = new();
        private readonly Mock<DataDbContextFactory> dbContextFactory = new();
        private static readonly TdaStockQuote tdaStockQuote = new()
        {
            Symbol = "SPY",
            QuoteTime = 1616785585000
        };
        private readonly TdaStockQuote testQuote = tdaStockQuote;
        private readonly TdaStockQuote fleshedOutQuote = new()
        {
            Symbol = "spy",
            Nope = 11.21F,
            Mark = 400.20F,
            QuoteTime = todayHours.Open + 30000,
            TotalCallOptionDelta = 4023404,
            TotalPutOptionDelta = 2342401,
            LocalCallOptionDelta = 123124,
            LocalPutOptionDelta = 1245341,
            TotalVolume = 1245542,
        };

        public NopeDataServiceTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(GetType().Assembly, typeof(Startup).Assembly);
            });
            memoryCache.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());
            dataService = new NopeDataService(cass.Object, redisCache.Object, memoryCache.Object, environment.Object, logger.Object, config.CreateMapper(), dbContextFactory.Object);
        }

        [Fact]
        public async void TestUpdateCache()
        {
            environment.SetupGet(x => x.EnvironmentName).Returns("Production");
            var response = new List<TdaStockQuote> { testQuote };
            cass.Setup(x => x.FetchQuotesForApiAsync(It.IsAny<string>(), It.IsAny<MarketHours>())).ReturnsAsync(response);
            await dataService.UpdateCache(testQuote);
            redisCache.Verify(cache => cache.SetAsync("SPY-0-3/26/2021", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async void TestUpdateCacheForTodayWithNoSpecifiedDate()
        {
            environment.SetupGet(x => x.EnvironmentName).Returns("Production");
            var quote = new TdaStockQuote() { Symbol = "SPY", QuoteTime = todayHours.Open };
            var response = new List<TdaStockQuote> { quote };
            cass.Setup(x => x.FetchQuotesForApiAsync(It.IsAny<string>(), It.IsAny<MarketHours>())).ReturnsAsync(response);
            await dataService.UpdateCache(quote);
            redisCache.Verify(cache => cache.SetAsync($"SPY-0-{todayHours.Date:M/d/yyyy}", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        } 
        [Fact]
        public async void TestDoesNotUpdateCacheWithNoResults()
        {
            environment.SetupGet(x => x.EnvironmentName).Returns("Production");
            var response = new List<TdaStockQuote> { };
            cass.Setup(x => x.FetchQuotesForApiAsync(It.IsAny<string>(), It.IsAny<MarketHours>())).ReturnsAsync(response);
            await dataService.UpdateCache(testQuote);
            redisCache.Verify(cache => cache.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async void TestUsesCacheInProduction()
        {
            environment.SetupGet(x => x.EnvironmentName).Returns("Production");
            var response = new List<TdaStockQuote>();
            cass.Setup(x => x.FetchQuotesForApiAsync(It.IsAny<string>(), It.IsAny<MarketHours>())).ReturnsAsync(response);
            await dataService.GetJsonAsync("SPY", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), "", todayHours);
            redisCache.Verify(cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async void TestDoesNotReadCacheInDev()
        {
            environment.SetupGet(x => x.EnvironmentName).Returns("Development");
            var response = new List<TdaStockQuote>();
            cass.Setup(x => x.FetchQuotesForApiAsync(It.IsAny<string>(), It.IsAny<MarketHours>())).ReturnsAsync(response);
            await dataService.GetJsonAsync("SPY", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), "", todayHours);
            redisCache.Verify(cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async void TestDoesNotCacheEmptyResults()
        {
            environment.SetupGet(x => x.EnvironmentName).Returns("Production");
            var response = new List<TdaStockQuote>();
            cass.Setup(x => x.FetchQuotesForApiAsync(It.IsAny<string>(), It.IsAny<MarketHours>())).ReturnsAsync(response);
            await dataService.GetJsonAsync("SPY", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), "", todayHours);
            redisCache.Verify(cache => cache.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async void TestDoesCacheWhenResultsReturned()
        { 
            environment.SetupGet(x => x.EnvironmentName).Returns("Production");
            var response = new List<TdaStockQuote>();
            response.Add(new TdaStockQuote());
            redisCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[])null);
            cass.Setup(x => x.FetchQuotesForApiAsync(It.IsAny<string>(), It.IsAny<MarketHours>())).ReturnsAsync(response);
            await dataService.GetJsonAsync("SPY", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), "", todayHours);
            redisCache.Verify(cache => cache.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(),It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async void TestUsesDateInsteadOfQuotetime()
        {
            var response = new List<TdaStockQuote>();
            redisCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[])null);
            cass.Setup(x => x.FetchQuotesForApiAsync(It.IsAny<string>(), It.IsAny<MarketHours>())).ReturnsAsync(response);
            await dataService.GetJsonAsync("SPY", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), todayHours.Date, todayHours);
            cass.Verify(x => x.FetchQuotesForApiAsync(It.IsAny<string>(), todayHours));
        }

        [Fact]
        public async void TestReturnsSubsetOfFields()
        {
            var response = new List<TdaStockQuote>();
            response.Add(fleshedOutQuote);
            redisCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[])null);
            cass.Setup(x => x.FetchQuotesForApiAsync(It.IsAny<string>(), It.IsAny<MarketHours>())).ReturnsAsync(response);
            var jsonResponse = await dataService.GetJsonAsync("SPY", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), todayHours.Date, todayHours);
            Assert.Equal($"{{\"dataType\":\"quotes\",\"data\":{{\"info\":{{\"symbol\":\"spy\",\"description\":null}},\"quotes\":[{{\"high\":0,\"low\":0,\"open\":0,\"close\":0,\"mark\":400.2,\"quoteTime\":{fleshedOutQuote.QuoteTime},\"totalVolume\":1245542,\"nope\":11.21,\"totalPutOptionDelta\":2342401,\"totalCallOptionDelta\":4023404,\"localPutOptionDelta\":1245341,\"localCallOptionDelta\":123124,\"localVolume\":0}}]}},\"success\":true}}", jsonResponse);
            var responseObject = JsonSerializer.Deserialize<V1Response<APIQuotes>>(jsonResponse);
            Assert.Equal("APIQuotes", responseObject.DataType);
        }
    }
}
