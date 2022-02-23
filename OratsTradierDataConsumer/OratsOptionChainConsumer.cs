namespace OratsTradierDataConsumer
{
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Azure.Storage.Queues;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using StockDataLibrary;
    using StockDataLibrary.Db;
    using StockDataLibrary.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class OratsOptionChainConsumer : BackgroundService
    {
        private readonly ILogger<OratsOptionChainConsumer> _logger;
        private readonly CopeCalculator copeCalculator = new();
        private readonly string azureBlobKeyConnectionString;
        private readonly string queueName;
        private readonly string deadletterQueueName;
        private readonly Dictionary<string, int> symbolCache = new();
        private readonly IDbContextFactory<DataDbContext> _dbContextFactory;
        private readonly object _locker = new();

        public OratsOptionChainConsumer(ILogger<OratsOptionChainConsumer> logger, ChonkyConfiguration config, IDbContextFactory<DataDbContext> dbContextFactory)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
            azureBlobKeyConnectionString = config.AzureBlobKeyConnectionString;
            queueName = config.OratsOptionChainQueueName;
            deadletterQueueName = config.OratsDeadletterQueueName;
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
                        _logger.LogError(e.InnerException.Message);
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
            copeCalculator.Calculate(optionChain);
            using var dbContext = _dbContextFactory.CreateDbContext();
            lock(_locker)
            {
                if (symbolCache.TryGetValue(optionChain.Symbol, out var id))
                {
                    optionChain.StockId = id;
                } 
                else
                {
                    var stock = dbContext.FindOrCreateStock(optionChain.Symbol);
                    symbolCache[optionChain.Symbol] = stock.Id;
                    optionChain.StockId = stock.Id;
                }
            }
            await dbContext.OratsOptionChains
                .Upsert(optionChain)
                .On(v => new { v.StockId, v.QuoteDate })
                .WhenMatched(v => new OratsOptionChain {
                    LocalCallOptionDelta = v.LocalCallOptionDelta,
                    LocalPutOptionDelta = v.LocalPutOptionDelta,
                    TotalCallOptionDelta = v.TotalCallOptionDelta,
                    TotalPutOptionDelta = v.TotalPutOptionDelta })
                .RunAsync();
        }

        private async ValueTask<OratsOptionChain> FetchOptionChainDataAsync(string message)
        {
            var containerName = message.Split("/")[0];
            var fileName = String.Join('/', message.Split("/")[1..]);
            var container = new BlobContainerClient(azureBlobKeyConnectionString, containerName);
            if (!await container.ExistsAsync())
                throw new Exception("Container referenced in message does not exist");

            var jsonBlob = container.GetBlobClient(fileName);
            if (!await jsonBlob.ExistsAsync())
                throw new Exception("Referenced json file does not exist");

            BlobDownloadInfo response = await jsonBlob.DownloadAsync();

            if (response.ContentLength < 500)
                throw new Exception($"Content length too small to contain data");

            return OratsOptionChain.ConstructFromGzippedOratsCsv(response.Content, message);
        }
    }
}
