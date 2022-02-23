namespace TDAStreamer
{
    using Azure.Messaging.ServiceBus;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using StockDataLibrary;
    using StockDataLibrary.Services;
    using StockDataLibrary.TDAmeritrade;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ITDAWebSocketClient _client;
        private readonly ChonkyConfiguration _config;
        private readonly List<string> tickers;
        private readonly IServiceBusProvider _serviceBusProvider;
        private readonly Dictionary<string, ServiceBusSender> _serviceBusSenders = new();
        private readonly Channel<DataContent> dataQueue = Channel.CreateUnbounded<DataContent>();
        private readonly Dictionary<string, StreamWriter> writers = new();
        private readonly bool TestMode = false;

        public Worker(ILogger<Worker> logger, ChonkyConfiguration config, ITDAWebSocketClient client, IServiceBusProvider serviceBusProvider)
        {
            _logger = logger;
            _config = config;
            _client = client;
            tickers = config.Tickers;
            _serviceBusProvider = serviceBusProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            foreach (var ticker in tickers)
            {
                var serviceBusSender = await _serviceBusProvider.GetServiceBusSender(_config.ServiceBusTradesTopic(ticker), true);
                _serviceBusSenders[ticker] = serviceBusSender;
            }
        
            if (!TestMode)
            {
                await _client.EstablishStreamingConnection(MessageHandler, stoppingToken);
                _client.ReceivedDataHandler += DatumHandler;
                await _client.JuiceIt(stoppingToken);
                await _client.SubscribeToEquity(tickers, stoppingToken);
                foreach (var ticker in tickers)
                {
                    writers[ticker] = File.CreateText($"{ticker}-{DateTime.Now.ToFileTimeUtc()}.json");
                    writers[ticker].AutoFlush = true;
                }

                while (!stoppingToken.IsCancellationRequested)
                {
                    var dataElements = dataQueue.Reader.ReadAllAsync(stoppingToken);
                    await foreach (var elem in dataElements)
                    {
                        var jsonContents = JsonSerializer.Serialize(elem);
                        await writers[elem.Key].WriteLineAsync(jsonContents);
                        var message = new ServiceBusMessage(jsonContents);
                        await _serviceBusSenders[elem.Key].SendMessageAsync(message, stoppingToken);
                    }
                    await Task.Delay(10, stoppingToken);
                }

                foreach (var writer in writers.Values)
                {
                    await writer.FlushAsync();
                    writer.Close();
                }
            }
            else
            {
                _ = Task.Run(async () =>
                {
                    while (true)
                    {
                        var reader = File.ReadAllText($"SPY.json");
                        var lines = reader.Split("\n");
                        foreach (var line in lines)
                        {
                            var msg = new ServiceBusMessage(line);
                            await _serviceBusSenders["SPY"].SendMessageAsync(msg);
                            await Task.Delay(5000, stoppingToken);
                        }
                    }
                }, stoppingToken);
            }
        }

        private void MessageHandler(object sender, Response message)
        {
            _logger.LogInformation(message.Command);
        }

        private void DatumHandler(object sender, DataResponse message)
        {
            foreach (var datum in message.Content)
            {
                dataQueue.Writer.TryWrite(datum);
            }
            _logger.LogInformation(message.Content.Count.ToString());
        }
    }
}
