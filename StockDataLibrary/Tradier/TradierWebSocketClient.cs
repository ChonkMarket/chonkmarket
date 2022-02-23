namespace StockDataLibrary.Tradier
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.WebSockets;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ITradierWebSocketClient
    {
        Task EstablishStreamingConnection(CancellationToken token);
        EventHandler<string> MessageHandler { get; set; }

    }

    public class TradierWebSocketClient : ITradierWebSocketClient
    {
        private readonly HttpClient _httpClient = new();
        private readonly ILogger<TradierWebSocketClient> _logger;
        private readonly ChonkyConfiguration _chonkyConfiguration;
        private ClientWebSocket _socketClient;
        private string _sessionId;
        private readonly List<string> tickers;
        public EventHandler<string> MessageHandler { get; set; }

        public TradierWebSocketClient(ILogger<TradierWebSocketClient> logger, ChonkyConfiguration chonkyConfiguration)
        {
            _logger = logger;
            _chonkyConfiguration = chonkyConfiguration;
            // tickers = _chonkyConfiguration.Tickers;
            tickers = new List<string> { "QQQ", "SPY" };
        }

        public async Task EstablishStreamingConnection(CancellationToken token)
        {
            try
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.tradier.com/v1/markets/events/session");
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _chonkyConfiguration.TradierAPIToken);
                requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var resp = await _httpClient.SendAsync(requestMessage, token);
                var responseContents = await resp.Content.ReadAsStringAsync(token);
                var sessionInfo = JsonSerializer.Deserialize<SessionCreateResponse>(responseContents);
                _sessionId = sessionInfo.stream.sessionid;

                _socketClient = new ClientWebSocket();
                await _socketClient.ConnectAsync(new Uri("wss://ws.tradier.com/v1/markets/events"), token);

                _ = Task.Run(async () =>
                {
                    var responseBuffer = new ArraySegment<byte>(new byte[2048]);

                    while (!token.IsCancellationRequested && _socketClient.State != WebSocketState.Closed)
                    {
                        WebSocketReceiveResult result;
                        using MemoryStream ms = new();
                        do
                        {
                            result = await _socketClient.ReceiveAsync(responseBuffer, token);
                            ms.Write(responseBuffer.Array, responseBuffer.Offset, result.Count);
                        } while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Close)
                            break;

                        ms.Seek(0, SeekOrigin.Begin);
                        var sr = new StreamReader(ms);
                        _logger.LogInformation(sr.ReadToEnd());
                        ms.Seek(0, SeekOrigin.Begin);
                        try
                        {
                            MessageHandler?.Invoke(this, sr.ReadToEnd());
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e.Message);
                        }
                    }
                }, token);

                var request = new RequestObject(tickers, _sessionId);
                var jsonRequest = JsonSerializer.Serialize(request);
                _logger.LogInformation(jsonRequest);
                var bytes = Encoding.UTF8.GetBytes(jsonRequest);
                var buffer = new ArraySegment<byte>(bytes, 0, bytes.Length);
                await _socketClient.SendAsync(buffer, WebSocketMessageType.Text, true, token);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }
    }

    public class SessionCreateResponse
    {
        public SessionCreateStream stream { get; set; }
    }

    public class SessionCreateStream
    {
        public string url {get;set;}
        public string sessionid { get; set; }
    }

    public class RequestObject
    {
        public List<string> symbols { get; set; }
        public string sessionid { get; set; }
        public List<string> filter { get; set; } = new() { "trade", "timesale" };

        public RequestObject(List<string> _symbols, string _sessionId)
        {
            this.symbols = _symbols;
            this.sessionid = _sessionId;
        }
    }
}
