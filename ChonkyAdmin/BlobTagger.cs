namespace ChonkyAdmin
{
    using Azure.Storage.Blobs;
    using Azure.Storage.Queues;
    using StockDataLibrary;
    using System;
    using System.Collections.Generic;

    class BlobTagger
    {
        private readonly string azureBlobKeyConnectionString;
        private readonly string queueName;
        private readonly QueueClient queueClient;
        private readonly List<string> tickers;
        private readonly Dictionary<string, Dictionary<string, string>> tags;
        private readonly ChonkyConfiguration _config;

        public BlobTagger(ChonkyConfiguration config)
        {
            _config = config;
            azureBlobKeyConnectionString = config.AzureBlobKeyConnectionString;
            queueName = config.OptionChainQueueName;
            queueClient = new QueueClient(azureBlobKeyConnectionString, queueName);
            queueClient.CreateIfNotExists();
            tags = new Dictionary<string, Dictionary<string, string>>();
            tickers = config.Tickers;
        }

        public void Run()
        {
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

            foreach (var ticker in tickers)
            {
                if (ticker == "SPY" || ticker == "GME" || ticker == "ARKK")
                    continue;
                var containerName = (_config.ProductionVersion(_config.ContainerName) + $"-{ticker}").ToLower();
                var container = new BlobContainerClient(azureBlobKeyConnectionString, containerName);

                var blobs = container.GetBlobs();
                foreach (var blob in blobs)
                {
                    Console.WriteLine(blob.Name);
                    var splitName = blob.Name.Split("-");
                    var time = Convert.ToInt64(splitName[1]) * 1000;
                    var date = dtDateTime.AddMilliseconds(time).ToString("M/d/yyyy");
                    var client = container.GetBlobClient(blob.Name);
                    if (!tags.TryGetValue(date, out var writeTags))
                    {
                        writeTags = new Dictionary<string, string>();
                        writeTags.Add("date", date);
                        tags.Add(date, writeTags);
                    }
                    client.SetTags(writeTags);
                }
            }
        }
    }
}
