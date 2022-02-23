namespace DataConsumer
{
    using Azure.Messaging.ServiceBus;
    using Azure.Storage.Queues;
    using Microsoft.Extensions.Hosting;
    using StockDataLibrary;
    using StockDataLibrary.Db;
    using System.Collections.Concurrent;
    using System.Text.Json;
    using System.Net.Http;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using StockDataLibrary.Models;

    public class BackgroundQuoteProcessing : BackgroundService
    {
        private readonly ICass _cassandra;
        private readonly ServiceBusSender serviceBusSender;
        private readonly ServiceBusClient serviceBusClient;

        private readonly string azureBlobKeyConnectionString;
        private readonly ConcurrentQueue<TdaOptionChain> _workQueue;
        private readonly bool updateCache;
        private readonly HttpClient client = new();
        private readonly string webHostName;
        private readonly CopeCalculator cnopeCalculator = new();
        private readonly string processedChainResultQueueName;
        private readonly JsonSerializerOptions options = new();
        private QueueClient processedChainResultQueueClient;

        public BackgroundQuoteProcessing(ChonkyConfiguration config, ICass cassandra, ConcurrentQueue<TdaOptionChain> workQueue)
        {
            azureBlobKeyConnectionString = config.AzureBlobKeyConnectionString;
            _cassandra = cassandra;
            _workQueue = workQueue;
            serviceBusClient = new ServiceBusClient(config.ServiceBusConnectionString);
            serviceBusSender = serviceBusClient.CreateSender(config.ServiceBusQuoteTopic);
            updateCache = config.UpdateCache;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
            processedChainResultQueueName = config.ProcessedChainResultQueueName;
            webHostName = config.WebHostName;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            processedChainResultQueueClient = new QueueClient(azureBlobKeyConnectionString, processedChainResultQueueName);
            await processedChainResultQueueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_workQueue.TryDequeue(out var optionChain))
                {
                    cnopeCalculator.Calculate(optionChain);
                    await _cassandra.StoreQuoteAsync(optionChain.Underlying);
                    if (updateCache)
                    {
                        var marketHours = TradingHours.GetMarketHours(optionChain.Underlying.QuoteTime);
                        _ = Task.Run(() => UpdateCacheAsync(optionChain.Symbol, marketHours.Date), stoppingToken);
                    }
                    var jsonText = JsonSerializer.Serialize(optionChain.Underlying, options);
                    processedChainResultQueueClient.SendMessage(jsonText, stoppingToken);
                    await serviceBusSender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(optionChain.Underlying)), stoppingToken);
                }
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        private Task UpdateCacheAsync(string symbol, string date)
        {
            client.GetAsync($"http://{webHostName}/api/nope/{symbol}/updatecache?date={HttpUtility.UrlEncode(date)}");
            return Task.CompletedTask;
        }
    }
}
