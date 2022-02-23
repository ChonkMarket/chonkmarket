namespace StockDataLibrary.Orats
{
    using Microsoft.Extensions.Logging;
    using StockDataLibrary.TDAmeritrade;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    public class ApiClient : IDataStreamerClient
    {
        private readonly ILogger<ApiClient> _logger;
        private readonly ChonkyConfiguration _chonkyConfiguration;
        private readonly string _oratsApiKey;
        private static readonly HttpClient client = new();

        public ApiClient(ChonkyConfiguration chonkyConfiguration, ILogger<ApiClient> logger)
        {
            _logger = logger;
            _chonkyConfiguration = chonkyConfiguration;
            _oratsApiKey = _chonkyConfiguration.ORATSKey;
        }

        public async Task<Stream> FetchOptionsStream(string ticker)
        {
            var response = await client.GetAsync($"https://api.orats.io/datav2/live/strikes.csv?token={_oratsApiKey}&ticker={ticker}");

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                return stream;
            }

            _logger.LogError("Received non-OK status code from ORATS API");
            _logger.LogError($"Status Code: {response.StatusCode}");
            _logger.LogError($"Response: {await response.Content.ReadAsStringAsync()}");
            throw new Exception("Failed to fetch from ORATS API");
        }

        public async Task<Stream> MakeRequest(string uri, bool retrying = false)
        {
            return await (await client.GetAsync(uri)).Content.ReadAsStreamAsync();
        }
    }
}
