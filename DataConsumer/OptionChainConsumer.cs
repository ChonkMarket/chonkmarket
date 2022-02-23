namespace DataConsumer
{
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Azure.Storage.Queues;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using StockDataLibrary.Db;
    using System;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Text.Json.Serialization;
    using StockDataLibrary;
    using System.Collections.Concurrent;
    using StockDataLibrary.Models;

    public class OptionChainConsumer : BackgroundService
    {
        private readonly ILogger<OptionChainConsumer> _logger;
        private readonly string azureBlobKeyConnectionString;
        private readonly string queueName;
        private readonly string deadletterQueueName;
        private readonly JsonSerializerOptions options = new();
        private readonly ICass _cassandra;
        private readonly ConcurrentQueue<TdaOptionChain> _backgroundQueue;

        public OptionChainConsumer(ILogger<OptionChainConsumer> logger, ChonkyConfiguration config, ICass cassandra, ConcurrentQueue<TdaOptionChain> backgroundQueue)
        {
            _logger = logger;
            azureBlobKeyConnectionString = config.AzureBlobKeyConnectionString;
            _cassandra = cassandra;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
            queueName = config.OptionChainQueueName;
            deadletterQueueName = config.DeadletterQueueName;
            _backgroundQueue = backgroundQueue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            QueueClient queueClient = new(azureBlobKeyConnectionString, queueName);
            if (!queueClient.Exists(stoppingToken))
            {
                throw new Exception($"{queueName} does not exist, exiting");
            }

            QueueClient deadletterQueueClient = new(azureBlobKeyConnectionString, deadletterQueueName);
            await deadletterQueueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            while (!stoppingToken.IsCancellationRequested)
            {
                var response = (await queueClient.ReceiveMessageAsync(TimeSpan.FromSeconds(30), stoppingToken)).Value;
                if (response != null)
                {
                    var message = response.Body.ToString();
                    try
                    {
                        await ProcessMessage(message);
                        queueClient.DeleteMessage(response.MessageId, response.PopReceipt, stoppingToken);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Failed to process message: ID: {response.MessageId} - {message}");
                        _logger.LogError(e.Message);
                        await deadletterQueueClient.SendMessageAsync($"Error: {e.Message}\n{message}", stoppingToken);
                        queueClient.DeleteMessage(response.MessageId, response.PopReceipt, stoppingToken);
                    }
                }
                else
                {
                    await Task.Delay(100, stoppingToken);
                }
            }
        }

        private async Task ProcessMessage(string message)
        {
            var optionChain = await FetchOptionChainDataAsync(message);
            await _cassandra.StoreQuoteAsync(optionChain);
            _backgroundQueue.Enqueue(optionChain);
        }

        private async ValueTask<TdaOptionChain> FetchOptionChainDataAsync(string message)
        {
            var containerName = message.Split("/")[0];
            var fileName = message.Split("/")[1];
            var container = new BlobContainerClient(azureBlobKeyConnectionString, containerName);
            if (!await container.ExistsAsync())
                throw new Exception("Container referenced in message does not exist");

            var jsonBlob = container.GetBlobClient(fileName);
            if (!await jsonBlob.ExistsAsync())
                throw new Exception("Referenced json file does not exist");

            BlobDownloadInfo response = await jsonBlob.DownloadAsync();

            if (response.ContentLength < 500)
                throw new Exception($"Content length too small to contain data");

            return await TdaOptionChain.ConstructFromJsonAsync(response.Content, message);
        }
    }
}
