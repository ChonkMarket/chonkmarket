namespace StockDataLibrary.Db
{
    using Cassandra;
    using Cassandra.Data.Linq;
    using Cassandra.Mapping;
    using StockDataLibrary.Models;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Extensions.Logging;

    public interface ICass
    {
        Task StoreQuoteAsync(TdaOptionChain optionChain);
        Task<List<TdaStockQuote>> FetchQuotesAsync(string ticker, MarketHours hours);
        Task<List<TdaStockQuote>> FetchQuotesForApiAsync(string ticker, MarketHours hours);
        Task<TdaStockQuote> FetchQuoteAsync(string ticker, long quotetime);
        Task<TdaStockQuote> FetchPreviousQuoteAsync(string ticker, MarketHours hours);
        Task StoreQuoteAsync(TdaStockQuote quote);
    }

    public class Cass : ICass
    {
        private readonly ChonkyConfiguration _config;
        private readonly ILogger _logger;
        private readonly string keyspace;
        private IMapper mapper;
        private ISession session;
        
        public Cass(ChonkyConfiguration config, ILogger<Cass> logger)
        {
            _logger = logger;
            _config = config;
            keyspace = _config.CassandraKeyspace;
            CassandraConnect();
        }

        public bool CassandraConnect()
        {
            var options = new SSLOptions(System.Security.Authentication.SslProtocols.Tls12, true, ValidateServerCertificate);
            options.SetHostNameResolver((ipAddress) => _config.CassandraContactPoint);
            MappingConfiguration.Global.Define<TdaQuoteMappings>();
            var cluster = Cluster.Builder()
                .AddContactPoint(_config.CassandraContactPoint)
                .WithCredentials(_config.CassandraUsername, _config.CassandraPassword)
                .WithDefaultKeyspace(keyspace)
                .WithPort(_config.CassandraPort)
                .WithSSL(options)
                .Build();
            session = cluster.ConnectAndCreateDefaultKeyspaceIfNotExists();
            mapper = new Mapper(session);
            var table = new Table<TdaStockQuote>(session);
            table.CreateIfNotExists();
            return true;
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            _logger.LogError("Certificate error: {0}", sslPolicyErrors);
            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        public async Task StoreQuoteAsync(TdaOptionChain optionChain)
        {
            var quote = optionChain.Underlying;
            quote.RawData = optionChain.RawData;
            await mapper.UpdateAsync<TdaStockQuote>(quote);
            _logger.LogInformation($"Stored data for {optionChain.Symbol} @ {optionChain.Underlying.QuoteTime}");
        }

        public async Task StoreQuoteAsync(TdaStockQuote quote)
        {
            await mapper.UpdateAsync<TdaStockQuote>(quote);
        }
        public async Task<List<TdaStockQuote>> FetchQuotesForApiAsync(string ticker, MarketHours hours)
        {
            var chains = await mapper.FetchAsync<TdaStockQuote>($"SELECT symbol, mark, nope, quotetime, totalcalloptiondelta, totalputoptiondelta, totalvolume, localcalloptiondelta, localputoptiondelta, localvolume FROM {TdaStockQuote.CASS_TABLE_NAME} WHERE symbol = ? AND quotetime > ? AND quotetime < ?", ticker, hours.Open, hours.Close);
            return chains.ToList();
        }

        public async Task<List<TdaStockQuote>> FetchQuotesAsync(string ticker, MarketHours hours)
        {
            var chains = await mapper.FetchAsync<TdaStockQuote>("WHERE symbol = ? AND quotetime > ? AND quotetime < ?", ticker, hours.Open, hours.Close);
            return chains.ToList();
        }

        public async Task<TdaStockQuote> FetchPreviousQuoteAsync(string ticker, MarketHours hours)
        {
            if (hours.Close >= hours.Open)
                return null;
            var chains = await mapper.FetchAsync<TdaStockQuote>("WHERE symbol = ? AND quotetime > ? AND quotetime < ?", ticker, hours.Open, hours.Close);
            return chains.Last();
        }

        public async Task<TdaStockQuote> FetchQuoteAsync(string ticker, long quotetime)
        {
            var chains = await mapper.FetchAsync<TdaStockQuote>("WHERE symbol = ? AND quotetime = ?", ticker, quotetime);
            return chains.Last();
        }
    }
}
