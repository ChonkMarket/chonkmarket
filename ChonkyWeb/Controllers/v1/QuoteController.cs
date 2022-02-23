namespace ChonkyWeb.Controllers.v1
{
    using ChonkyWeb.Models.V1ApiModels;
    using ChonkyWeb.Modelsl.V1ApiModels;
    using ChonkyWeb.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using StockDataLibrary;
    using StockDataLibrary.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class QuoteController : ExternalApiController
    {
        private readonly ILogger<QuoteController> _logger;
        private readonly ChonkyConfiguration _chonkyConfiguration;
        private readonly SSEClientManager _sseClientManager;
        private readonly INopeDataService _dataService;
        private readonly TestDataSenderFactory _dataSenderFactory;
        private readonly long maxQueryRange;

        public QuoteController(ILogger<QuoteController> logger, ChonkyConfiguration chonkyConfiguration, SSEClientManager sseClientManager, INopeDataService dataService, TestDataSenderFactory dataSenderFactory)
        {
            _logger = logger;
            _chonkyConfiguration = chonkyConfiguration;
            _sseClientManager = sseClientManager;
            _dataService = dataService;
            maxQueryRange = ChonkyConfiguration.MAX_QUERY_RANGE;
            _dataSenderFactory = dataSenderFactory;
        }

        /// <summary>
        /// Fetch quotes, nope, and volume data for a given symbol.
        /// </summary><remarks>
        /// This data is cached for up to a few minutes, so you should always follow this up with a subsequent call
        /// with <c>quotetime</c> set to the most recent event received if you are fetching data while the market is
        /// still open for the day you're retrieving data for.
        /// </remarks>
        /// <param name="symbol">The symbol of the underlying stock</param>
        /// <param name="quotetime">Fetch data starting from this quotetime until end of that day.</param>
        /// <param name="date">Format is "M/d/yyyy" (4/6/2021), returns all data for this date.</param>
        [ProducesResponseType(typeof(V1Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(V1Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(V1Response<APIQuotes>), StatusCodes.Status200OK)]
        [HttpGet("{symbol}")]
        public async Task<IActionResult> GetQuotes([FromRoute] string symbol, [FromQuery] long quotetime = 0, [FromQuery] string date = "")
        {
            symbol = symbol.ToUpper();
            if (_chonkyConfiguration.Tickers.IndexOf(symbol) == -1)
            {
                return new JsonResult(GenerateErrorResponse($"Invalid Ticker: {symbol}"))
                {
                    StatusCode = StatusCodes.Status404NotFound
                };
            }
            var hours = new MarketHours(quotetime, quotetime + maxQueryRange);
            if (date != "")
            {
                hours = TradingHours.GetMarketHours(date);
                if (hours.Date != date)
                {
                    return new JsonResult(GenerateErrorResponse($"Market Closed on {date}"))
                    {
                        StatusCode = StatusCodes.Status404NotFound 
                    };
                }
            }
            else if (quotetime == 0)
            {
                return new JsonResult(GenerateErrorResponse("Please specify either a date or a quotetime."))
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            var quotes = await _dataService.GetJsonAsync(symbol, quotetime, date, hours);
            var result = Content(quotes, "application/json");
            result.StatusCode = StatusCodes.Status200OK;
            return result;
        }

        /// <summary>
        /// Returns the list of currently supported tickers.
        /// </summary>
        [ProducesResponseType(typeof(V1Response<List<string>>), StatusCodes.Status200OK)]
        [HttpGet("tickers")]
        public V1Response GetTickers()
        {
            return GenerateSuccessResponse(_chonkyConfiguration.Tickers, "Array<string>");
        }

        /// <summary>
        /// Establishes a streaming Server Side Events connection for a given Symbol.
        /// </summary>
        /// <remarks>
        /// Example of the stream:
        ///
        ///     event: market_close
        ///     data: 1617998400000
        ///     
        ///     event: ping
        ///     data: ping
        ///     
        ///     event: ping
        ///     data: ping
        ///
        ///     data: {"mark":335.455, "quoteTime":1617985872446, "totalVolume":17746040, "totalPutOptionDelta":-93700.65909840644, "totalCallOptionDelta":105483.19072863832, "nope":6.639527258042855, "localPutOptionDelta":-536.11, "localCallOptionDelta":743.213, "localVolume":803}
        ///
        /// <para>First response after connection will always be a market_close event, this is when data will stop flowing
        /// and you can disconnect.</para>
        ///
        /// <para>The API will send pings every 10s to keep the connection alive, these can be disregarded.</para>
        ///
        /// <para>Note that try it out below will not work, Swagger doesn't handle SSE connections.</para>
        /// </remarks>
        /// <param name="symbol">The symbol of the underlying stock</param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpGet("{symbol}/sse")]
        public async Task SSE([FromRoute] string symbol)
        {
            var context = HttpContext;
            var hours = TradingHours.GetMarketHours(DateTime.Now);
            var client = _sseClientManager.RegisterClient(symbol, context);
            await client.SendEventAsync("market_close", hours.Close.ToString());
            await client.Task;
        }

        /// <summary>
        /// Establishes a test streaming Server Side Events connection for a given Symbol.
        /// </summary>
        /// <remarks>
        /// <para>See <see cref="SSE(string)"/> for general details</para>
        /// <para>This mode will replay data for a specific date to test out the stream. This will only stream data for
        /// completed days. If you want to explore live data for today, the regular endpoint is intended for that.</para>
        /// <para></para>
        /// <para>Unlike the main stream, it will not send out pings, it will just stream you quotes.</para>
        /// <para></para>
        /// <para>Interval rate defaults to sending a quote down the stream every second, but this can be reduced
        /// down to a minum of 200ms per quote.</para>
        /// </remarks>
        /// <param name="symbol">The symbol of the underlying stock</param>
        /// <param name="date">The date of data to stream</param> 
        /// <param name="interval">Rate (in milliseconds) to send updates</param> 
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet("{symbol}/sse/test")]
        public async Task SSETest([FromRoute] string symbol, string date, int interval = 1000)
        {
            if (interval < 200)
            {
                BadRequest();
                return;
            }
            var hours = TradingHours.GetMarketHours(date);
            if (hours.Date != date)
            {
                BadRequest();
                return;
            }
            if (hours.Close > DateTimeOffset.Now.ToUnixTimeMilliseconds())
            {
                BadRequest();
                return;
            }
            var testDataSender = _dataSenderFactory.GenerateTestDataSender(HttpContext, symbol, date, interval);
            await testDataSender.Task;
        }
    }
}
