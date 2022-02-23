namespace ChonkyWeb.Services
{
    using AutoMapper;
    using Azure.Messaging.ServiceBus;
    using Azure.Messaging.ServiceBus.Administration;
    using ChonkyWeb.Models.V1ApiModels;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using StockDataLibrary;
    using StockDataLibrary.Models;
    using StockDataLibrary.Protos;
    using StockDataLibrary.TDAmeritrade;
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public class UpdateListener : IHostedService
    {
        private readonly SSEClientManager _clientManager;
        private readonly WsSocketManager _wsSocketManager;
        private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
        private ServiceBusClient _serviceBusClient;
        private ServiceBusAdministrationClient _serviceBusAdminClient;
        private ServiceBusProcessor chonkCacheUpdateProcessor;
        private ServiceBusProcessor chonkQuoteUpdateProcessor;
        private readonly Dictionary<string, ServiceBusProcessor> tradeProcessors = new();
        private readonly INopeDataService _dataService;
        private readonly ILogger<UpdateListener> _logger;
        private readonly Task _initialized;
        private string _subscriptionName;
        private readonly string _topicName;
        private readonly IMapper _mapper;
        private readonly ChonkyConfiguration _chonkyConfiguration;

        public UpdateListener(SSEClientManager clientManager, ILogger<UpdateListener> logger, ChonkyConfiguration config, INopeDataService dataService, IMapper mapper, WsSocketManager wsSocketManager)
        {
            _clientManager = clientManager;
            _chonkyConfiguration = config;
            _logger = logger;
            _topicName = config.ServiceBusQuoteTopic;
            _dataService = dataService;
            _wsSocketManager = wsSocketManager;
            _mapper = mapper;
            _initialized = Task.Run(async () =>
            {
                // ok, this feels a little complicated
                // the cache update processor is tied to an 'official' subscription that will only allow 1
                // consumer to receive a message. this means that 1 web worker somewhere will update the cache
                // for a given symbol
                //
                // then we also generate our own subscription to ensure that we get all stock updates pushed to us
                // so we can then push them out via SSE to any connected clients. this subscription should cease
                // to exist 5 minutes after we exit so we don't create too many of these
                //
                _serviceBusClient = new ServiceBusClient(config.ServiceBusConnectionString);
                chonkCacheUpdateProcessor = _serviceBusClient.CreateProcessor(_topicName, config.ServiceBusChonkyUpdateSubscription);
                chonkCacheUpdateProcessor.ProcessMessageAsync += ChonkCacheUpdateProcessor_ProcessMessageAsync;
                chonkCacheUpdateProcessor.ProcessErrorAsync += ProcessErrorAsync;

                _serviceBusAdminClient = new ServiceBusAdministrationClient(config.ServiceBusConnectionString);
                _subscriptionName = Guid.NewGuid().ToString();
                CreateSubscriptionOptions options = new(_topicName, _subscriptionName)
                {
                    AutoDeleteOnIdle = TimeSpan.FromMinutes(5),
                    DefaultMessageTimeToLive = TimeSpan.FromMinutes(5),
                    SubscriptionName = _subscriptionName
                };
                await _serviceBusAdminClient.CreateSubscriptionAsync(options);

                // need to update tradier consumer to push trades
                //
                // await SubscribeToTickersAsync(options);

                chonkQuoteUpdateProcessor = _serviceBusClient.CreateProcessor(config.ServiceBusQuoteTopic, _subscriptionName);
                chonkQuoteUpdateProcessor.ProcessMessageAsync += ChonkQuoteUpdateProcessor_ProcessMessageAsync;
                chonkQuoteUpdateProcessor.ProcessErrorAsync += ProcessErrorAsync;
            });
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _wsSocketManager.CancellationToken = cancellationToken;
            await _initialized;
            await chonkCacheUpdateProcessor.StartProcessingAsync(cancellationToken);
            await chonkQuoteUpdateProcessor.StartProcessingAsync(cancellationToken);
            //foreach (var ticker in _chonkyConfiguration.Tickers)
            //{
            //    await tradeProcessors[ticker].StartProcessingAsync(cancellationToken);
            //}
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _serviceBusAdminClient.DeleteSubscriptionAsync(_topicName, _subscriptionName, cancellationToken);
        }

        private Task ProcessErrorAsync(ProcessErrorEventArgs arg)
        {
            _logger.LogError(arg.Exception.ToString());
            return Task.CompletedTask;
        }

        private async Task ChonkCacheUpdateProcessor_ProcessMessageAsync(ProcessMessageEventArgs arg)
        {
            var quote = JsonSerializer.Deserialize<TdaStockQuote>(arg.Message.Body);
            await _dataService.UpdateCache(quote);
            await arg.CompleteMessageAsync(arg.Message);
        }

        private Task ChonkQuoteUpdateProcessor_ProcessMessageAsync(ProcessMessageEventArgs arg)
        {
            var contents = JsonSerializer.Deserialize<TdaStockQuote>(arg.Message.Body);
            if (contents.IsMarketOpen())
            {
                var serializedContents = JsonSerializer.Serialize(_mapper.Map<Quote>(contents), jsonOptions);
                _clientManager.PushData(contents.Symbol, serializedContents);
            }
            return arg.CompleteMessageAsync(arg.Message);
        }

        private async Task SubscribeToTickersAsync(CreateSubscriptionOptions options)
        {
            foreach (var ticker in _chonkyConfiguration.Tickers)
            {
                var topicName = _chonkyConfiguration.ServiceBusTradesTopic(ticker).ToLower();
                try
                {
                    options.TopicName = topicName;
                    await _serviceBusAdminClient.CreateSubscriptionAsync(options);
                    tradeProcessors[ticker] = _serviceBusClient.CreateProcessor(options.TopicName, _subscriptionName);
                    tradeProcessors[ticker].ProcessErrorAsync += ProcessErrorAsync;
                    tradeProcessors[ticker].ProcessMessageAsync += async (ProcessMessageEventArgs arg) =>
                    {
                        var datum = JsonSerializer.Deserialize<DataContent>(arg.Message.Body);
                        var trade = new Trade()
                        {
                            Last = datum.Last,
                            Size = datum.Size,
                            TradeTime = datum.TradeTime
                        };
                        _wsSocketManager.PushTrade(ticker, trade);
                        await arg.CompleteMessageAsync(arg.Message);
                    };
                }
                catch (Exception e)
                {
                    _logger.LogError($"Failed to subscribe to {topicName}");
                    _logger.LogError(e.Message);
                }
            }
        }
    }
}
