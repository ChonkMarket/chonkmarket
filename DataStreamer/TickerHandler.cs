namespace DataStreamer
{
    using Azure.Storage.Blobs;
    using Azure.Storage.Queues;
    using Microsoft.Extensions.Logging;
    using StockDataLibrary;
    using StockDataLibrary.Models;
    using StockDataLibrary.TDAmeritrade;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    class TickerHandler
    {
        private readonly string queueName;
        private readonly string containerName;
        private readonly string azureBlobKeyConnectionString;
        private readonly string ticker;
        private readonly IDataStreamerClient _client;
        private readonly ILogger<TickerHandler> _logger;
        private Task task;
        private readonly DateTime epochTime;
        private readonly Dictionary<string, Dictionary<string, string>> tags;

        public TickerHandler(ChonkyConfiguration config, IDataStreamerClient client, ILogger<TickerHandler> logger, string _ticker)
        {
            _client = client;
            _logger = logger;
            azureBlobKeyConnectionString = config.AzureBlobKeyConnectionString;
            queueName = config.OptionChainQueueName;
            containerName = config.ContainerName;
            ticker = _ticker;
            if (string.IsNullOrEmpty(ticker))
            {
                throw new Exception("No ticker passed in, exiting");
            }
            _logger.LogInformation($"Worker started to process data for {ticker}");
            containerName = (containerName + $"-{ticker}").ToLower();
            tags = new Dictionary<string, Dictionary<string, string>>();
            epochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        }

        public Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var container = new BlobContainerClient(azureBlobKeyConnectionString, containerName);
            container.CreateIfNotExists(cancellationToken: stoppingToken);
            var queueClient = new QueueClient(azureBlobKeyConnectionString, queueName);
            queueClient.CreateIfNotExists(cancellationToken: stoppingToken);

            task = Task.Run(async () =>
            {
                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var now = DateTimeOffset.Now;
                        var unixNow = now.ToUnixTimeSeconds();

                        var filename = $"{ticker}-{unixNow}-{Guid.NewGuid()}.json";
                        BlobClient blob = container.GetBlobClient(filename);
                        var date = epochTime.AddSeconds(unixNow).ToString("M/d/yyyy");
                        if (!tags.TryGetValue(date, out var writeTags))
                        {
                            writeTags = new Dictionary<string, string>
                            {
                                { "date", date }
                            };
                            tags.Add(date, writeTags);
                        }

                        var stream = await _client.FetchOptionsStream(ticker);
                        await blob.UploadAsync(stream);
                        blob.SetTags(writeTags);

                        queueClient.SendMessage($"{containerName}/{filename}");
                        _logger.LogInformation($"Processed {filename}");

                        // 30s from the time we started this last loop
                        var timeToDelay = Math.Max(0, 30000 - (DateTimeOffset.Now.ToUnixTimeMilliseconds() - now.ToUnixTimeMilliseconds()));

                        _logger.LogDebug($"Sleeping for {timeToDelay} milliseconds");
                        await Task.Delay(Convert.ToInt32(timeToDelay), stoppingToken);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.ToString());
                    throw;
                }
            }, stoppingToken);
            return task;
        }
    }
}
