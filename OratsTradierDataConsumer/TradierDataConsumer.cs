namespace OratsTradierDataConsumer
{
    using Azure.Storage.Queues;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using StockDataLibrary;
    using StockDataLibrary.Db;
    using StockDataLibrary.Models;
    using StockDataLibrary.Tradier.StreamingModels;
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public class TradierDataConsumer : BackgroundService
    {
        private readonly ILogger<TradierDataConsumer> _logger;
        private readonly ChonkyConfiguration _chonkyConfiguration;
        private CancellationToken _stoppingToken;
        private QueueClient tradeQueueClient;
        private QueueClient timesaleQueueClient;
        private QueueClient deadletterQueueClient;
        private readonly MarketHours today = TradingHours.GetMarketHours(DateTime.Now);
        private readonly Dictionary<long, TradierSnapshot> spySnapshots = new();
        private readonly Dictionary<long, TradierSnapshot> qqqSnapshots = new();
        private Stock spy;
        private Stock qqq;
        private readonly DataDbContext dbContext;
        private object _locker = new();

        public TradierDataConsumer(ILogger<TradierDataConsumer> logger, ChonkyConfiguration config, IDbContextFactory<DataDbContext> dbContextFactory)
        {
            _logger = logger;
            _chonkyConfiguration = config;
            dbContext = dbContextFactory.CreateDbContext();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timesaleQueueClient = new(_chonkyConfiguration.AzureBlobKeyConnectionString, _chonkyConfiguration.TradierTimesaleQueueName);
            if (!timesaleQueueClient.Exists(stoppingToken))
            {
                throw new Exception($"{_chonkyConfiguration.TradierTimesaleQueueName} does not exist, exiting");
            }

            tradeQueueClient = new(_chonkyConfiguration.AzureBlobKeyConnectionString, _chonkyConfiguration.TradierTradeQueueName);
            if (!tradeQueueClient.Exists(stoppingToken))
            {
                throw new Exception($"{_chonkyConfiguration.TradierTradeQueueName} does not exist, exiting");
            }

            deadletterQueueClient = new(_chonkyConfiguration.AzureBlobKeyConnectionString, _chonkyConfiguration.TradierDeadletterQueueName);
            await deadletterQueueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

            _stoppingToken = stoppingToken;

            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            spy = dbContext.FindOrCreateStock("SPY");
            qqq = dbContext.FindOrCreateStock("QQQ");

            _ = Task.Run(async () => { await ProcessQueue<Timesale>(timesaleQueueClient); }, stoppingToken);
            _ = Task.Run(async () => { await ProcessQueue<Trade>(tradeQueueClient); }, stoppingToken);
            _ = Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await dbContext.SaveChangesAsync();
                    await Task.Delay(10000);
                }
            }, stoppingToken);
        }

        private async Task ProcessQueue<T>(QueueClient client) where T : ITradierStreamingData
        {
            var jsonOptions = new JsonSerializerOptions()
            {
                AllowTrailingCommas = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };
            while (!_stoppingToken.IsCancellationRequested)
            {
                var response = (await client.ReceiveMessageAsync(TimeSpan.FromSeconds(10), _stoppingToken)).Value;
                if (response != null)
                {
                    var message = response.Body.ToString();
                    if (message == null)
                        break;
                    try
                    {
                        List<T> messages = JsonSerializer.Deserialize<List<T>>(message, jsonOptions);
                        foreach (var entity in messages)
                            ProcessMessage(entity);
                        client.DeleteMessage(response.MessageId, response.PopReceipt, _stoppingToken);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Failed to process message: ID: {response.MessageId} - {message}");
                        _logger.LogError(e.Message);
                        await deadletterQueueClient.SendMessageAsync($"Error: {e.Message}\n{message}");
                        client.DeleteMessage(response.MessageId, response.PopReceipt, _stoppingToken);
                    }
                }
                else
                {
                    await Task.Delay(1000);
                }
            }
        }

        private void ProcessMessage(ITradierStreamingData message)
        {
            var snapshots = message.Symbol == "SPY" ? spySnapshots : qqqSnapshots;
            if (message.Date > today.Open && message.Date < today.Close)
            {
                var snaptime = message.Snaptime;
                lock (_locker)
                {
                    if (snapshots.TryGetValue(snaptime, out var snapshot))
                    {
                        message.AddToSnapshot(snapshot);
                    }
                    else
                    {
                        TradierSnapshot trade = message.CreateSnapshot();
                        trade.SymbolId = message.Symbol == "SPY" ? spy.Id : qqq.Id;
                        dbContext.TradierSnapshots.Add(trade);
                        snapshots[snaptime] = trade;
                    }
                }
            }
        }
    }
}
