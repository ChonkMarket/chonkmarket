namespace StockDataLibrary.TDAmeritrade
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    public interface ITDAClient
    {
        Task<Stream> FetchOptionsStream(string ticker);
        Task<Stream> MakeRequest(string uri, bool retrying = false);
    }

    public class TDAClient : IDataStreamerClient
    {
        private readonly string apiKey;
        private string accessToken;
        private readonly string refreshToken;
        private readonly string clientId;
        private readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance
        };
        private static readonly HttpClient client = new();
        private readonly ILogger<TDAClient> _logger;
        private Task refreshTokenTask;
        private static readonly object _locker = new();

        public TDAClient(ChonkyConfiguration config, ILogger<TDAClient> logger)
        {
            accessToken = config.TdaAccessToken;
            clientId = config.TdaClientId;
            apiKey = config.TdaApiKey;
            refreshToken = config.TdaRefreshToken;
            _logger = logger;
        }

        public async Task<Stream> FetchOptionsStream(string ticker)
        {
            var response = await MakeRequest($"https://api.tdameritrade.com/v1/marketdata/chains?apikey={apiKey}&includeQuotes=true&symbol={ticker}");
            return response;
        }

        private Task RefreshTokenAsync()
        {
            _logger.LogDebug("Calling refresh token");
            lock (_locker)
            {
                if (refreshTokenTask != null && !refreshTokenTask.IsCompleted)
                {
                    return refreshTokenTask;
                }
                refreshTokenTask = Task.Run(async () =>
                {
                    _logger.LogDebug("Going to fetch new refresh token");
                    HttpRequestMessage message = new(HttpMethod.Post, $"https://api.tdameritrade.com/v1/oauth2/token");

                    StringContent content = new($"client_id={clientId}&grant_type=refresh_token&refresh_token={refreshToken}", Encoding.UTF8, "application/x-www-form-urlencoded");
                    message.Content = content;

                    var contentBody = message.Content.ToString();

                    HttpResponseMessage response = await client.SendAsync(message).ConfigureAwait(false);

                    var tokenResponse = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>(jsonOptions);
                    accessToken = tokenResponse.AccessToken;
                });
            }
            return refreshTokenTask;
       }
        public async Task<Stream> MakeRequest(string uri, bool retrying = false)
        {
            HttpRequestMessage message = new(HttpMethod.Get, uri);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.SendAsync(message);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (retrying == false)
                {
                    await RefreshTokenAsync();
                    return await MakeRequest(uri, true);
                }
                throw new Exception("Unable to authorize client");
            }

            var stream = await response.Content.ReadAsStreamAsync();
            // occasionally we get back failure messages with small (264 byte) bodies. to avoid deserializing everything to check for that, we're just going to try again if we get a small response
            //
            if (stream.Length < 500)
            {
                if (retrying == false)
                {
                    using (StreamReader sr = new(stream))
                        _logger.LogError($"Tiny response - {sr.ReadToEnd()}, retrying");
                    return await MakeRequest(uri, true);
                }
                throw new Exception("Only receiving small body responses");
            }
            return stream;
        }
    }
}
