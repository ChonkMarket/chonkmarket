namespace TradierDataStreamer
{
    using Azure.Storage.Blobs;
    using Azure.Storage.Queues;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using StockDataLibrary;
    using StockDataLibrary.Services;
    using StockDataLibrary.Tradier;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ChonkyConfiguration _chonkyConfiguration;
        private readonly List<string> tickers;
        private readonly ITradierWebSocketClient _socketClient;
        private readonly Dictionary<string, StreamWriter> writers = new();
        private readonly Channel<string> dataQueue = Channel.CreateUnbounded<string>();
        private readonly ConcurrentQueue<string> tradeBatcher = new();
        private readonly ConcurrentQueue<string> timesaleBatcher = new();
        private CancellationToken _stoppingToken;

        public Worker(ILogger<Worker> logger, ChonkyConfiguration chonkyConfiguration, IServiceBusProvider serviceBusProvider, ITradierWebSocketClient socketClient)
        {
            _logger = logger;
            _chonkyConfiguration = chonkyConfiguration;
            tickers = chonkyConfiguration.Tickers;
            _socketClient = socketClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _stoppingToken = stoppingToken;
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            await _socketClient.EstablishStreamingConnection(stoppingToken);
            _socketClient.MessageHandler += MessageHandler;
            _logger.LogInformation("Connected to Tradier");

            if (!_chonkyConfiguration.IsProduction)
            {
                _ = Task.Run(WriteDataToDisk, stoppingToken);
            }
            else
            {
                _ = Task.Run(ProcessJSON, stoppingToken);
                _ = Task.Run(SendData, stoppingToken);
            }
        }

        private void MessageHandler(object sender, string message)
        {
            dataQueue.Writer.TryWrite(message);
        }

        private async Task WriteDataToDisk()
        {
            foreach (var ticker in tickers)
            {
                writers[ticker] = File.CreateText($"{ticker}-{DateTime.Now.ToFileTimeUtc()}.json");
            }
            writers["default"] = File.CreateText($"default-{DateTime.Now.ToFileTimeUtc()}.json");

            while (!_stoppingToken.IsCancellationRequested)
            {
                var dataElements = dataQueue.Reader.ReadAllAsync(_stoppingToken);
                await foreach (var elem in dataElements)
                {
                    bool matched = false;
                    foreach (var ticker in tickers)
                    {
                        if (elem.Contains(ticker))
                        {
                            matched = true;
                            await writers[ticker].WriteLineAsync(elem);
                            break;
                        }
                    }
                    if (!matched)
                        await writers["default"].WriteLineAsync(elem);
                }
                await Task.Delay(10, _stoppingToken);
            }
        }

        private async Task ProcessJSON()
        {
            while (!_stoppingToken.IsCancellationRequested)
            {
                var dataElements = dataQueue.Reader.ReadAllAsync(_stoppingToken);
                await foreach (var elem in dataElements)
                {
                    if (elem.Contains("\"trade\""))
                    {
                        tradeBatcher.Enqueue(elem);
                    }
                    if (elem.Contains("\"timesale\""))
                    {
                        timesaleBatcher.Enqueue(elem);
                    }
                }
                await Task.Delay(10, _stoppingToken);
            }
        }

        private async Task SendData()
        {
            var container = new BlobContainerClient(_chonkyConfiguration.AzureBlobKeyConnectionString, _chonkyConfiguration.ContainerName);
            container.CreateIfNotExists(cancellationToken: _stoppingToken);
            var tradeQueueClient = new QueueClient(_chonkyConfiguration.AzureBlobKeyConnectionString, _chonkyConfiguration.TradierTradeQueueName);
            tradeQueueClient.CreateIfNotExists(cancellationToken: _stoppingToken);
            var timesaleQueueClient = new QueueClient(_chonkyConfiguration.AzureBlobKeyConnectionString, _chonkyConfiguration.TradierTimesaleQueueName);
            timesaleQueueClient.CreateIfNotExists(cancellationToken: _stoppingToken);

            while (!_stoppingToken.IsCancellationRequested)
            {
                if (!tradeBatcher.IsEmpty)
                {
                    _logger.LogInformation($"Consuming trade queue, {tradeBatcher.Count} messages waiting");
                    var tradeString = ConsumeQueue(tradeBatcher);
                    await tradeQueueClient.SendMessageAsync(tradeString);
                }
                if (!timesaleBatcher.IsEmpty)
                {
                    _logger.LogInformation($"Consuming timesale queue, {timesaleBatcher.Count} messages waiting");
                    var timesaleString = ConsumeQueue(timesaleBatcher);
                    await timesaleQueueClient.SendMessageAsync(timesaleString);
                }
                await Task.Delay(200, _stoppingToken);
            }
        }

        private string ConsumeQueue(ConcurrentQueue<string> queue)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append('[');
            var max = Math.Min(50, queue.Count);
            for (var i = 0; i < max; i++)
            {
                if (i > 0)
                    stringBuilder.Append(',');
                if (queue.TryDequeue(out var elem))
                    stringBuilder.Append(elem);
            }
            stringBuilder.Append(']');
            return stringBuilder.ToString();
        }
    }
}
