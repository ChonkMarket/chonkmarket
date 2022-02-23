namespace ChonkyWeb.Services
{
    using AutoMapper;
    using ChonkyWeb.Models.V1ApiModels;
    using Microsoft.AspNetCore.Http;
    using StockDataLibrary;
    using StockDataLibrary.Db;
    using StockDataLibrary.Models;
    using StockDataLibrary.TDAmeritrade;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public class TestDataSenderFactory
    {
        private ICass _cass;
        private IMapper _mapper;

        public TestDataSenderFactory(ICass cass, IMapper mapper)
        {
            _cass = cass;
            _mapper = mapper;
        }

        public TestDataSender GenerateTestDataSender(HttpContext context, string symbol, string date, int interval)
        {
            var hours = TradingHours.GetMarketHours(date);
            var client = new SSEClient(context);
            return new TestDataSender(client, symbol, _cass, hours, interval, _mapper);
        }
    }
    public class TestDataSender : IDisposable
    {
        private SSEClient _client;
        private ICass _cass;
        private IMapper _mapper;
        private string _symbol;
        private MarketHours _marketDayHours;
        private long _interval;
        private CancellationTokenSource _tokenSource = new();
        private TaskCompletionSource _taskCompletionSource = new();
        public Task Task { get => _taskCompletionSource.Task; }
        private static JsonSerializerOptions jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        public TestDataSender(SSEClient client, string symbol, ICass cass, MarketHours hours, long interval, IMapper mapper)
        {
            _client = client;
            _symbol = symbol;
            _cass = cass;
            _marketDayHours = hours;
            _mapper = mapper;
            _interval = interval;
            client.ConnectionClosed += (e, client) => { this.Dispose(); };
            _ = StartDataLoop();
        }
        
        private async Task StartDataLoop()
        {
            await _client.SendEventAsync("market_close", _marketDayHours.Close.ToString());

            // we fetch data for an hour at a time
            // queryHours stores the next query
            // if queryHours open is greater than the marketDayHours close and we have nothing left to send
            // then we're done here
            // ideally we ship off a quote every interval milliseconds down the pipe until we sent it all
            //
            await Task.Run(async () =>
            {
                var cancellationToken = _tokenSource.Token;
                Queue<TdaStockQuote> quotesToSend = new();
                var queryHours = TradingHours.GetMarketHours(_marketDayHours.Date);
                queryHours.Close = queryHours.Open + 3600000;

                while (!cancellationToken.IsCancellationRequested)
                {
                    var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    if (quotesToSend.Count == 0)
                    {
                        var newQuotes = await FetchDataAsync(queryHours);
                        newQuotes.ForEach(o => quotesToSend.Enqueue(o));
                        queryHours.Open = quotesToSend.Count > 0 ? quotesToSend.Last().QuoteTime : queryHours.Close;
                        queryHours.Close = queryHours.Open + 3600000;
                        if (queryHours.Close > _marketDayHours.Close)
                        {
                            queryHours.Close = _marketDayHours.Close;
                        }
                    }
                    if (quotesToSend.Count > 0)
                    {
                        var quote = quotesToSend.Dequeue();
                        await _client.SendDataAsync(JsonSerializer.Serialize(_mapper.Map<Quote>(quote), jsonOptions));
                        var timeToDelay = Math.Max(0, _interval - (DateTimeOffset.Now.ToUnixTimeMilliseconds() - now));
                        await Task.Delay((int)timeToDelay, cancellationToken);
                    }
                    if (quotesToSend.Count == 0 && queryHours.Open >= _marketDayHours.Close)
                        break;
                }
            });
            this.Dispose();
        }

        private Task<List<TdaStockQuote>> FetchDataAsync(MarketHours hours)
        {
            return _cass.FetchQuotesForApiAsync(_symbol, hours);
        }

        public void Dispose()
        {
            _tokenSource.Cancel();
            _client = null;
        }
    }
}
