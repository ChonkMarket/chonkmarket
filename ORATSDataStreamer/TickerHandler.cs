namespace OratsDataStreamer
{
    using Azure.Storage.Blobs;
    using Azure.Storage.Queues;
    using Microsoft.Extensions.Logging;
    using StockDataLibrary;
    using StockDataLibrary.TDAmeritrade;
    using System;
    using System.IO;
    using System.IO.Compression;
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

        public TickerHandler(ChonkyConfiguration config, IDataStreamerClient client, ILogger<TickerHandler> logger, string _ticker)
        {
            _client = client;
            _logger = logger;
            azureBlobKeyConnectionString = config.AzureBlobKeyConnectionString;
            queueName = config.OratsOptionChainQueueName;
            containerName = config.OratsContainerName;
            ticker = _ticker;
            if (string.IsNullOrEmpty(ticker))
            {
                throw new Exception("No ticker passed in, exiting");
            }
            _logger.LogInformation($"Worker started to process data for {ticker}");
            containerName = (containerName + $"-{ticker}").ToLower();
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
                        var dateString = now.ToString("yyyy-MM-dd");

                        var filename = $"{dateString}/{ticker}-{unixNow}-{Guid.NewGuid()}.csv.gz";
                        BlobClient blob = container.GetBlobClient(filename);

                        var stream = await _client.FetchOptionsStream(ticker);
                        var memoryStream = new MemoryStream();
                        GZipStream compressionStream = new(memoryStream, CompressionLevel.Optimal);
                        stream.CopyTo(compressionStream);
                        await stream.FlushAsync();
                        await compressionStream.FlushAsync();
                        memoryStream.Seek(0, 0);
                        await blob.UploadAsync(memoryStream);

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
