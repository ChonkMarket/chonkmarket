namespace OratsDataStreamer
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using StockDataLibrary.TDAmeritrade;
    using System.Collections.Generic;
    using StockDataLibrary;

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDataStreamerClient _client;
        private readonly ChonkyConfiguration _config;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly List<Task> workers = new();
        private readonly List<string> tickers;

        public Worker(ILogger<Worker> logger, ChonkyConfiguration config, IDataStreamerClient client, ILoggerFactory loggerFactory, IHostApplicationLifetime hostApplicationLifetime)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _logger = logger;
            _client = client;
            _config = config;
            _loggerFactory = loggerFactory;
            tickers = config.Tickers;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_config.IsProduction)
            {
                await CheckMarket(stoppingToken);
            }
            //foreach (string ticker in tickers)
            //{
            //    var handler = new TickerHandler(_config, _client, _loggerFactory.CreateLogger<TickerHandler>(), ticker);
            //    workers.Add(BufferCall(handler, stoppingToken));
            //}
            var handler = new TickerHandler(_config, _client, _loggerFactory.CreateLogger<TickerHandler>(), "SPY");
            workers.Add(BufferCall(handler, stoppingToken));

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.WhenAny(workers.ToArray());
                _hostApplicationLifetime.StopApplication();
            }
            _logger.LogError("One of the workers failed, exiting");
        }

        private Task BufferCall(TickerHandler handler, CancellationToken stoppingToken)
        {
            try
            {
                return handler.ExecuteAsync(stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
            return Task.CompletedTask;
        }

        private async Task CheckMarket(CancellationToken stoppingToken)
        {
            var now = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
            var marketHours = TradingHours.GetMarketHours(now);
            if (now > marketHours.Close)
            {
                _logger.LogError("Market is closed, exiting");
                _hostApplicationLifetime.StopApplication();
            }
            if (now < marketHours.Open)
            {
                int waitTime = Convert.ToInt32(marketHours.Open - now);
                int minutes = waitTime / 1000 / 60;
                int seconds = minutes % 60;
                minutes -= seconds;

                _logger.LogWarning($"Market is not open yet, waiting {minutes}:{seconds} before polling starts");
                await Task.Delay(waitTime, stoppingToken);
            }
        }
    }
}
