namespace chonkyweb.Controllers
{
    using ChonkyWeb.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using StockDataLibrary;
    using StockDataLibrary.Models;
    using StockDataLibrary.Protos;
    using StockDataLibrary.TDAmeritrade;
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    [ApiController]
    public class NopeController : ControllerBase
    {
        private readonly ILogger<NopeController> _logger;
        private readonly SSEClientManager _sseClientManager;
        private readonly WsSocketManager _wsSocketManager;
        private readonly string tickers;
        private readonly INopeDataService _dataService;
        private readonly long maxQueryRange;
        private readonly long historicalDifference;

        public NopeController(ILogger<NopeController> logger, 
            ChonkyConfiguration config, SSEClientManager sseClientManager,
            INopeDataService dataService, WsSocketManager wsSocketManager)
        {
            _sseClientManager = sseClientManager;
            _logger = logger;
            _dataService = dataService;
            _wsSocketManager = wsSocketManager;
            tickers = JsonSerializer.Serialize(config.Tickers);
            maxQueryRange = ChonkyConfiguration.MAX_QUERY_RANGE;
            historicalDifference = ChonkyConfiguration.HISTORICAL_DIFFERENCE;
        }

        [HttpGet("{symbol}")]
        public async Task<ContentResult> GetNope([FromRoute] string symbol, [FromQuery] long quotetime = 0, [FromQuery] string date = "")
        {
            symbol = symbol.ToUpper();
            var hours = new MarketHours(quotetime, quotetime + maxQueryRange);
            if (date != "")
            {
                hours = TradingHours.GetMarketHours(date);
            }
            else if (quotetime == 0)
            {
                hours = TradingHours.GetMarketHours(DateTime.Now);
            }

            // Limit data for non-premium users
            if (User == null || !User.IsInRole("Premium"))
            {
                _logger.LogInformation("Attempt to fetch old data by non-premium user");
                if (hours.Open < DateTimeOffset.Now.ToUnixTimeMilliseconds() - historicalDifference)
                    return Content("Requests for data older than 7 days are prohibited");
            }

            var quotes = await _dataService.GetJsonAsync(symbol, quotetime, date, hours);
            return Content(quotes, "application/json");
        }

        [HttpGet("candlestick/{symbol}")]
        public async Task<ContentResult> GetData([FromRoute] string symbol, [FromQuery] string date = "")
        {
            symbol = symbol.ToUpper();
            MarketHours hours = TradingHours.GetMarketHours(date);

            // Limit data for non-premium users
            if (User == null || !User.IsInRole("Premium"))
            {
                _logger.LogInformation("Attempt to fetch old data by non-premium user");
                if (hours.Open < DateTimeOffset.Now.ToUnixTimeMilliseconds() - historicalDifference)
                    return Content("Requests for data older than 7 days are prohibited");
            }

            var quotes = await _dataService.GetJsonNewDataAsync(symbol, hours);
            return Content(quotes, "application/json");
        }

        [HttpGet("tickers")]
        public ContentResult Get()
        {
            return Content(tickers, "application/json");
        }

        [HttpGet("{symbol}/sse")]
        public async Task SSE([FromRoute] string symbol, [FromQuery] long quotetime, [FromQuery] string date)
        {
            var context = HttpContext;
            var hours = TradingHours.GetMarketHours(date);
            hours.Open = quotetime;
            var quotes = await _dataService.GetJsonAsync(symbol, quotetime, hours.Date, hours);
            var client = _sseClientManager.RegisterClient(symbol, context);
            await client.SendDataAsync(quotes);
            await client.SendEventAsync("market_close", hours.Close.ToString());
            await client.Task;
        }

        [HttpGet("{symbol}/ws")]
        public async Task WS([FromRoute] string symbol, [FromQuery] long quotetime, [FromQuery] string date)
        {
            var context = HttpContext;
            if (!context.WebSockets.IsWebSocketRequest)
            {
                BadRequest();
                return;
            }
            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var managedSocket = _wsSocketManager.RegisterSocket(socket, symbol);
            await managedSocket.Task;
        }

        [HttpGet("{symbol}/ws/test")]
        public async Task WSTest()
        {
            var context = HttpContext;
            if (!context.WebSockets.IsWebSocketRequest)
            {
                BadRequest();
                return;
            }
            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var wsSocket = new WsSocket(socket, new System.Threading.CancellationToken());
            var reader = System.IO.File.ReadAllText($"SPY.json");
            var lines = reader.Split("\n");
            foreach (var line in lines)
            {
                var datum = JsonSerializer.Deserialize<DataContent>(line);
                var trade = new Trade()
                {
                    Last = datum.Last,
                    Size = datum.Size,
                    TradeTime = datum.TradeTime
                };
                await wsSocket.PushData(trade);
                await Task.Delay(10);
            }
            Ok();
            return;
        }
    }
}
