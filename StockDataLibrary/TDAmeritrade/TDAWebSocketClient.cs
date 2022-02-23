namespace StockDataLibrary.TDAmeritrade
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.WebSockets;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ITDAWebSocketClient
    {
        Task EstablishStreamingConnection(EventHandler<Response> eventHandler, CancellationToken token);
        Task SubscribeToOption(string option, CancellationToken token);
        Task SubscribeToEquity(List<string> equities, CancellationToken token);
        Task JuiceIt(CancellationToken token);
        EventHandler<DataResponse> ReceivedDataHandler { get; set; }
    }

    public class TDAWebSocketClient : ITDAWebSocketClient
    {
        private readonly ITDAClient _webClient;
        private readonly ILogger<TDAWebSocketClient> _logger;
        private UserPrincipal _userPrincipal;
        private ClientWebSocket _socketClient;
        private readonly TaskCompletionSource _loggedIn = new();
        public EventHandler<Response> ReceivedResponseHandler { get; set; }
        public EventHandler<DataResponse> ReceivedDataHandler { get; set; }

        public TDAWebSocketClient(ITDAClient webClient, ILogger<TDAWebSocketClient> logger)
        {
            _webClient = webClient;
            _logger = logger;
        }

        public async Task SubscribeToOption(string ticker, CancellationToken token)
        {
            ticker = ticker.ToUpper();
            var request = new Request(_userPrincipal)
            {
                Service = "OPTIONS_BOOK",
                Command = "VIEW"
            };
            request.Parameters["keys"] = ticker;
            request.Parameters["fields"] = "0,1,2,3,8,9,11,12,24,25,26,31,32,33,34,35,36,41";

            var jsonRequest = JsonSerializer.Serialize(new RequestObject(request));
            _logger.LogInformation(jsonRequest);
            var bytes = Encoding.UTF8.GetBytes(jsonRequest);
            var buffer = new ArraySegment<byte>(bytes, 0, bytes.Length);
            await _socketClient.SendAsync(buffer, WebSocketMessageType.Text, true, token);
        }
        public async Task SubscribeToEquity(List<string> equities, CancellationToken token)
        {
            var request = new Request(_userPrincipal)
            {
                Service = "TIMESALE_EQUITY",
                Command = "SUBS"
            };
            request.Parameters["keys"] = String.Join(',', equities);
            request.Parameters["fields"] = "0,1,2,3,4";

            var jsonRequest = JsonSerializer.Serialize(new RequestObject(request));
            _logger.LogInformation(jsonRequest);
            var bytes = Encoding.UTF8.GetBytes(jsonRequest);
            var buffer = new ArraySegment<byte>(bytes, 0, bytes.Length);
            await _socketClient.SendAsync(buffer, WebSocketMessageType.Text, true, token);
        }


        public async Task EstablishStreamingConnection(EventHandler<Response> eventHandler, CancellationToken token)
        {
            ReceivedResponseHandler = eventHandler;
            _userPrincipal = await FetchUserPrincipalAsync();
            var request = new Request(_userPrincipal)
            {
                Service = "ADMIN",
                Command = "LOGIN"
            };
            request.Parameters["credential"] = new Credentials(_userPrincipal).ToQueryStringParameters();
            request.Parameters["version"] = "1.0";
            request.Parameters["token"] = _userPrincipal.StreamerInfo.Token;

            _socketClient = new ClientWebSocket();
            await _socketClient.ConnectAsync(new Uri($"wss://{_userPrincipal.StreamerInfo.StreamerSocketUrl}/ws"), token);

            var jsonHandler = new JsonSerializerOptions
            {
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };

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
                        var response = await JsonSerializer.DeserializeAsync<ReceivedMessage>(ms, jsonHandler);
                        if (response.Responses?.Count > 0)
                        {
                            if (response?.Responses[0]?.Service == "ADMIN")
                                _loggedIn.SetResult();
                            else
                                response.Responses?.ForEach(resp => { ReceivedResponseHandler?.Invoke(this, resp); });
                        }

                        if (response.Data?.Count > 0)
                        {
                            response.Data?.ForEach(resp => { ReceivedDataHandler?.Invoke(this, resp);  });
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.Message);
                    }
                }
            }, token);

            var jsonRequest = JsonSerializer.Serialize(new RequestObject(request));
            _logger.LogInformation(jsonRequest);
            var bytes = Encoding.UTF8.GetBytes(jsonRequest);
            var buffer = new ArraySegment<byte>(bytes, 0, bytes.Length);
            await _socketClient.SendAsync(buffer, WebSocketMessageType.Text, true, token);
            await _loggedIn.Task;
        }



        private async Task<UserPrincipal> FetchUserPrincipalAsync()
        {
            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.Converters.Add(new DateTimeConverter());
            var response = await _webClient.MakeRequest("https://api.tdameritrade.com/v1/userprincipals?fields=streamerSubscriptionKeys%2CstreamerConnectionInfo");
            try
            {
                var responseObject = await JsonSerializer.DeserializeAsync<UserPrincipal>(response, jsonOptions);
                return responseObject;
            }
            catch (Exception e)
            {
                using (StreamReader sr = new(response))
                {
                    response.Position = 0;
                    _logger.LogError(sr.ReadToEnd());
                }
                _logger.LogError(e.Message);
                throw;
            }
        }

        public async Task JuiceIt(CancellationToken token)
        {
            var request = new Request(_userPrincipal)
            {
                Service = "ADMIN",
                Command = "QOS"
            };
            request.Parameters["quoslevel"] = "0";
            var jsonRequest = JsonSerializer.Serialize(new RequestObject(request));
            _logger.LogInformation(jsonRequest);
            var bytes = Encoding.UTF8.GetBytes(jsonRequest);
            var buffer = new ArraySegment<byte>(bytes, 0, bytes.Length);
            await _socketClient.SendAsync(buffer, WebSocketMessageType.Text, true, token);
        }
    }
}
