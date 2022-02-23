namespace ChonkyWeb.Services
{
    using AutoMapper;
    using ChonkyWeb.Models.V1ApiModels;
    using ChonkyWeb.Modelsl.V1ApiModels;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using StackExchange.Redis;
    using StockDataLibrary;
    using StockDataLibrary.Db;
    using StockDataLibrary.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class NopeDataService : INopeDataService
    {
        private readonly ICass _cassandra;
        private readonly IDistributedCache _redisCache;
        private readonly IWebHostEnvironment _environment;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<NopeDataService> _logger;
        private readonly IMapper _mapper;
        private JsonSerializerOptions jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        private readonly DistributedCacheEntryOptions redisCacheOptions;
        private readonly MemoryCacheEntryOptions memoryCacheOptions;
        private readonly IDbContextFactory<DataDbContext> _dbContextFactory;

        public NopeDataService(ICass cassandra, IDistributedCache distributedCache, IMemoryCache cache, IWebHostEnvironment environment, ILogger<NopeDataService> logger, IMapper mapper, IDbContextFactory<DataDbContext> dbContextFactory)
        {
            _cassandra = cassandra;
            _redisCache = distributedCache;
            _memoryCache = cache;
            _environment = environment;
            _logger = logger;
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;

            redisCacheOptions = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5));

            memoryCacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromSeconds(30));
        }

        public async Task<string> GetJsonAsync(string symbol, long quotetime, string date, MarketHours hours)
        {
            var cacheKey = $"{symbol}-{quotetime}-{date}";

            if (_memoryCache.TryGetValue(cacheKey, out string quotesJson))
            {
                return quotesJson;
            }

            if (_environment.IsProduction())
            {
                try
                {
                    var stocks = await _redisCache.GetAsync(cacheKey);
                    if (stocks != null)
                    {
                        quotesJson = Encoding.UTF8.GetString(stocks);
                        _memoryCache.Set(cacheKey, quotesJson, memoryCacheOptions);
                        return quotesJson;
                    }
                }
                catch (RedisTimeoutException e)
                {
                    _logger.LogError(e.ToString());
                }
            }

            var stockQuotes = await _cassandra.FetchQuotesForApiAsync(symbol, hours);
            quotesJson = SerializeQuotes(stockQuotes);
            await _updateCache(quotesJson, symbol, quotetime, date);

            return quotesJson;
        }

        public async Task UpdateCache(TdaStockQuote quote)
        {
            MarketHours hours = TradingHours.GetMarketHours(quote.QuoteTime);
            var stockQuotes = await _cassandra.FetchQuotesForApiAsync(quote.Symbol, hours);
            if (stockQuotes.Count > 0)
            {
                var quotesJson = SerializeQuotes(stockQuotes);
                await _updateCache(quotesJson, quote.Symbol, 0, hours.Date);
            }
        }

        private async Task _updateCache(string quotes, string symbol, long quotetime, string date)
        {
            var cacheKey = $"{symbol}-{quotetime}-{date}";

            _memoryCache.Set(cacheKey, quotes, memoryCacheOptions);
            if (_environment.IsProduction())
            {
                try
                {
                    await _redisCache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(quotes), redisCacheOptions);
                }
                catch (RedisTimeoutException e)
                {
                    _logger.LogError(e.ToString());
                }
            }
        }

        private string SerializeQuotes(List<TdaStockQuote> quotes)
        {
            var serializedData = _mapper.Map<List<TdaStockQuote>, APIQuotes>(quotes);
            var response = new V1Response();
            response.Data = serializedData;
            response.DataType = "quotes";
            response.Success = true;
            return JsonSerializer.Serialize(response, jsonOptions);
        }

        public async Task<string> GetJsonNewDataAsync(string symbol, MarketHours hours)
        {
            var cacheKey = $"{symbol}-{hours.Open}-{hours.Close}-new";

            if (_memoryCache.TryGetValue(cacheKey, out string quotesJson))
            {
                return quotesJson;
            }

            if (_environment.IsProduction())
            {
                try
                {
                    var stocks = await _redisCache.GetAsync(cacheKey);
                    if (stocks != null)
                    {
                        quotesJson = Encoding.UTF8.GetString(stocks);
                        _memoryCache.Set(cacheKey, quotesJson, memoryCacheOptions);
                        return quotesJson;
                    }
                }
                catch (RedisTimeoutException e)
                {
                    _logger.LogError(e.ToString());
                }
            }

            var db = _dbContextFactory.CreateDbContext();
            var stock = db.Stocks.Where(s => s.Symbol == symbol.ToUpper()).FirstOrDefault();
            if (stock == null)
            {
                _logger.LogInformation($"Tried to fetch data for unknown symbol {symbol}");
                return "";
            }

            var options = await db.OratsOptionChains.Where(o => o.StockId == stock.Id && o.QuoteDate >= hours.Open && o.QuoteDate <= hours.Close).OrderBy(o => o.QuoteDate).ToListAsync();
            var tradier = await db.TradierSnapshots.Where(o => o.SymbolId == stock.Id && o.Time >= hours.Open / 1000 && o.Time <= hours.Close / 1000).OrderBy(o => o.Time).ToListAsync();

            var apiResponse = new APIQuotes()
            {
                Info = new QuoteInfo()
                {
                    Symbol = stock.Symbol
                },
                Quotes = new List<Quote>()
            };

            long lastVol = 0;
            var optionIndex = 0;

            foreach (var trade in tradier)
            {
                var quote = new Quote()
                {
                    High = trade.High,
                    Low = trade.Low,
                    Open = trade.Open,
                    Close = trade.Close,
                    Mark = trade.Close,
                    QuoteTime = trade.Time,
                    TotalVolume = trade.CumulativeVolume,
                    LocalVolume = trade.CumulativeVolume - lastVol
                };
                var millisecondTime = trade.Time * 1000;
                lastVol = trade.CumulativeVolume;
                // is the current options index after the start of this trade interval?
                while (optionIndex < options.Count && options[optionIndex].QuoteDate <= trade.OpenTime)
                    optionIndex += 1;
                if (optionIndex != options.Count && options[optionIndex].QuoteDate < trade.CloseTime)
                {
                    var option = options[optionIndex];
                    quote.TotalCallOptionDelta = option.TotalCallOptionDelta;
                    quote.TotalPutOptionDelta = option.TotalPutOptionDelta;
                    quote.LocalCallOptionDelta = option.LocalCallOptionDelta;
                    quote.LocalPutOptionDelta = option.LocalPutOptionDelta;
                    quote.Nope = (option.TotalCallOptionDelta + option.TotalPutOptionDelta) * 10000 / trade.CumulativeVolume;
                } else
                {
                    continue;
                }
                apiResponse.Quotes.Add(quote);
            }

            var response = new V1Response();
            response.Data = apiResponse;
            response.DataType = "quotes";
            response.Success = true;
            return JsonSerializer.Serialize(response, jsonOptions);
        }
    }
}
