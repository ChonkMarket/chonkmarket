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

    class TestWorker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ChonkyConfiguration _chonkyConfiguration;
        private readonly Channel<string> dataQueue = Channel.CreateUnbounded<string>();
        private readonly ConcurrentQueue<string> tradeBatcher = new();
        private readonly ConcurrentQueue<string> timesaleBatcher = new();
        private CancellationToken _stoppingToken;

        public TestWorker(ILogger<Worker> logger, ChonkyConfiguration chonkyConfiguration, IServiceBusProvider serviceBusProvider)
        {
            _logger = logger;
            _chonkyConfiguration = chonkyConfiguration;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            var testData = File.ReadAllLines("default-132630784745196174.json");
            foreach (var jsonLine in testData)
            {
                MessageHandler(this, jsonLine);
            }
            var tasks = new List<Task>();
            tasks.Add(Task.Run(ProcessJSON, stoppingToken));
            tasks.Add(Task.Run(SendData, stoppingToken));

            return Task.WhenAny(tasks);
        }

        private void MessageHandler(object sender, string message)
        {
            dataQueue.Writer.TryWrite(message);
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
                    var tradeString = ConsumeQueue(tradeBatcher);
                    await tradeQueueClient.SendMessageAsync(tradeString);
                }
                if (!timesaleBatcher.IsEmpty)
                {
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
                if (queue.TryDequeue(out var elem))
                {
                    stringBuilder.Append(elem);
                }
                if (i < queue.Count)
                {
                    stringBuilder.Append(',');
                }
            }
            stringBuilder.Append(']');
            return stringBuilder.ToString();
        }
}
}
