namespace OratsTradierDataConsumer
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using StockDataLibrary;
    using StockDataLibrary.Db;
    using StockDataLibrary.Models;
    using StockDataLibrary.Tradier.StreamingModels;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public class TradierDataTester : BackgroundService
    {
        private readonly ILogger<TradierDataTester> _logger;
        private readonly Dictionary<string, int> symbolCache = new();
        private readonly IDbContextFactory<DataDbContext> _dbContextFactory;
        private readonly ChonkyConfiguration _chonkyConfiguration;
        private CancellationToken _stoppingToken;
        private readonly Dictionary<string, StreamReader> readers = new();
        private readonly MarketHours today = TradingHours.GetMarketHours(DateTime.Now);
        private readonly Dictionary<long, TradierSnapshot> spySnapshots = new();
        private readonly Dictionary<long, TradierSnapshot> qqqSnapshots = new();
        private Stock spy;
        private Stock qqq;

        public TradierDataTester(ILogger<TradierDataTester> logger, ChonkyConfiguration config, IDbContextFactory<DataDbContext> dbContextFactory)
        {
            _logger = logger;
            _chonkyConfiguration = config;
            _dbContextFactory = dbContextFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _stoppingToken = stoppingToken;

            readers["timesale"] = File.OpenText("timesale-132633389793253148.json");
            readers["trade"] = File.OpenText("trade-132633389793238713.json");

            await ProcessQueue<Timesale>(readers["timesale"]);
            await ProcessQueue<Trade>(readers["trade"]);
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                foreach (var snapshot in spySnapshots.Values)
                {
                    dbContext.TradierSnapshots.Add(snapshot);
                }
                foreach (var snapshot in qqqSnapshots.Values)
                {
                    dbContext.TradierSnapshots.Add(snapshot);
                }
                dbContext.SaveChanges();
            }
        }

        private async Task ProcessQueue<T>(StreamReader reader) where T : ITradierStreamingData
        {
            var jsonOptions = new JsonSerializerOptions()
            {
                AllowTrailingCommas = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                spy = dbContext.FindOrCreateStock("SPY");
                qqq = dbContext.FindOrCreateStock("QQQ");
            }
            while (!_stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = reader.ReadLine();
                    if (message == null)
                        break;
                    try
                    {
                        List<T> messages = JsonSerializer.Deserialize<List<T>>(message, jsonOptions);
                        foreach (var entity in messages)
                            await ProcessMessage(entity);
                    } catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                } catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private async Task ProcessMessage(ITradierStreamingData message)
        {
            var snapshots = message.Symbol == "SPY" ? spySnapshots : qqqSnapshots;
            if (message.Date > today.Open && message.Date < today.Close)
            {
                if (snapshots.TryGetValue(message.Snaptime, out var snapshot))
                {
                    message.AddToSnapshot(snapshot);
                } 
                else
                {
                    TradierSnapshot trade = message.CreateSnapshot();
                    trade.SymbolId = message.Symbol == "SPY" ? spy.Id : qqq.Id;
                    snapshots[message.Snaptime] = trade;
                }
            }
        }
    }
}
