namespace ChonkyAdmin
{
    using Azure.Storage.Blobs;
    using Azure.Storage.Queues;
    using StockDataLibrary;
    using System;
    using System.Globalization;

    class Restreamer
    {
        private readonly string azureBlobKeyConnectionString;
        private readonly string queueName;
        private readonly QueueClient queueClient;
        private readonly ChonkyConfiguration _config;

        public Restreamer(ChonkyConfiguration config) 
        {
            _config = config;
            azureBlobKeyConnectionString = config.AzureBlobKeyConnectionString;
            queueName = config.OptionChainQueueName;
            queueClient = new QueueClient(azureBlobKeyConnectionString, queueName);
            queueClient.CreateIfNotExists();
        }
        
        public void Run(string symbol, string day)
        {
            var containerName = (_config.ProductionVersion(_config.ContainerName) + $"-{symbol}").ToLower();
            var container = new BlobContainerClient(azureBlobKeyConnectionString, containerName);
            var hours = TradingHours.GetMarketHours(day);
            hours.Open = hours.Open - 60000;
            var blobs = container.GetBlobs();
            var count = 0;
            foreach (var blob in blobs)
            {
                var splitName = blob.Name.Split("-");
                var time = Convert.ToInt64(splitName[1]) * 1000;
                if (time > hours.Close)
                    continue;
                if (time > hours.Open)
                {
                    count += 1;
                    var messageToSend = $"{containerName}/{blob.Name}";
                    queueClient.SendMessage(messageToSend);
                }
                if (count % 100 == 0 && count > 0)
                    Console.WriteLine($"{count} blobs queued");
            }
            Console.WriteLine($"{count} blobs queued");
        }
    }
}
