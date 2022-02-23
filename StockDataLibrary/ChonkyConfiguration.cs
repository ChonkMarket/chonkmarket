namespace StockDataLibrary
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.AzureAppConfiguration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Sentry.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Web;

    public class ChonkyConfiguration
    {
        private readonly IHostEnvironment _env;

        /// <summary>
        /// 27000000 milliseconds (one full trading day)
        /// </summary>
        public const long MAX_QUERY_RANGE = 27000000;
        /// <summary>
        /// 691200000
        /// </summary>
        public const long HISTORICAL_DIFFERENCE = 691200000;
        public string CassandraContactPoint { get; } 
        public string CassandraUsername { get; }
        public string CassandraPassword { get; }
        public string CassandraKeyspace { get; }
        public int CassandraPort { get; }

        public string ProcessedChainResultQueueName { get; }
        public string OptionChainQueueName { get; }
        public string DeadletterQueueName { get; }
        public string ContainerName { get; }

        public string OratsProcessedChainResultQueueName { get; }
        public string OratsOptionChainQueueName { get; }
        public string OratsDeadletterQueueName { get; }
        public string OratsContainerName { get; }

        public string TradierTradeQueueName { get; }
        public string TradierTimesaleQueueName { get; }
        public string TradierDeadletterQueueName { get; }

        public string UserUpdateQueueName { get; }

        public string AzureBlobKeyConnectionString { get; }
        public string AzureTenantId { get; }
        public string AzureClientId { get; }
        public string AzureClientSecret { get; }
        public string AzureKeyVaultEndpoint { get; }

        public string ServiceBusConnectionString { get; }
        public string ServiceBusQuoteTopic { get; }
        public string ServiceBusChonkyUpdateSubscription { get; } = "chonky-cache-update";
        public string ServiceBusSubscriptionsTopic { get; }
        private string ServiceBusTradesTopicPattern { get; set; }

        public string DiscordBotToken { get; }
        public string DiscordClientId { get; }
        public string DiscordClientSecret { get; }


        public List<string> Tickers { get; }

        public string PostgresConnectionString { get; }
        public string DataPostgresConnectionString { get; }
        public string PostgresDatabaseName { get; }

        public string RedisConnectionString { get; }

        public string SendGridKey { get; }
        public bool UpdateCache { get; } = false;
        public string WebHostName { get; } = "chonky.market";

        public string TdaAccessToken { get; }
        public string TdaClientId { get; }
        public string TdaRefreshToken { get; }
        public string TdaApiKey { get; }
        public string ORATSKey { get; }
        public string TradierAPIToken { get; }

        public string StripePublicKey { get; }
        public string StripeSecretKey { get; }
        public string StripeSubscriptionPriceId { get; }

        public string WebServerHostName { get; }

        public int RefreshTokenTTLDays { get; } = 90;
        public string Secret { get; }
        public bool IsProduction { get; }

        public ChonkyConfiguration(IConfiguration config, IHostEnvironment env)
        {
            _env = env;
            string environmentLabel = EnvironmentLabel();

            UpdateCache = env.IsProduction();

            if (env.IsProduction())
            {
                IsProduction = true;
                WebServerHostName = "chonk.market";
            }
            else
            {
                IsProduction = false;
                WebServerHostName = "chonkmarket.dev:44396";
            }

            CassandraContactPoint = config["CassandraContactPoint"];
            CassandraUsername = config["CassandraUsername"];
            CassandraPassword = config["CassandraPassword"];
            CassandraPort = Convert.ToInt32(config["CassandraPort"]);
            CassandraKeyspace = $"chonky";

            ProcessedChainResultQueueName = $"{environmentLabel}processed-option-chain-results";
            OptionChainQueueName = $"{environmentLabel}option-chains-to-be-processed";
            DeadletterQueueName = $"{environmentLabel}failed-option-chains";
            ContainerName = $"{environmentLabel}raw-option-chain-data";

            OratsProcessedChainResultQueueName = $"{environmentLabel}orats-processed-option-chain-results";
            OratsOptionChainQueueName = $"{environmentLabel}orats-option-chains-to-be-processed";
            OratsDeadletterQueueName = $"{environmentLabel}orats-failed-option-chains";
            OratsContainerName = $"{environmentLabel}orats-raw-option-chain-data";

            TradierTradeQueueName = $"{environmentLabel}trades-to-be-processed";
            TradierTimesaleQueueName = $"{environmentLabel}timesales-to-be-processed";
            TradierDeadletterQueueName = $"{environmentLabel}tradier-deadletter";

            UserUpdateQueueName = $"{environmentLabel}user-update-queue";

            AzureBlobKeyConnectionString = config["AzureBlobKeyConnectionString"];
            AzureTenantId = config["AzureTenantId"];
            AzureClientId = config["AzureClientId"];
            AzureClientSecret = config["AzureClientSecret"];
            AzureKeyVaultEndpoint = config["AzureKeyVaultEndpoint"];

            ServiceBusConnectionString = config["ServiceBusConnectionString"];
            ServiceBusQuoteTopic = config["ServiceBusQuoteTopic"];
            ServiceBusSubscriptionsTopic = config["ServiceBusSubscriptionsTopic"];
            ServiceBusTradesTopicPattern = config["ServiceBusTradesTopicPattern"];

            DiscordBotToken = config["DiscordBotToken"];
            DiscordClientId = config["DiscordClientId"];
            DiscordClientSecret = config["DiscordClientSecret"];

            if (string.IsNullOrEmpty(config["tickers"]))
            {
                Tickers = new List<string>() { "SPY", "QQQ" };
            } else
            {
                Tickers = JsonSerializer.Deserialize<List<string>>(config["tickers"]);
            }

            PostgresConnectionString = config.GetConnectionString("DatabaseConnection");
            if (PostgresConnectionString == null)
                PostgresConnectionString = "";
            DataPostgresConnectionString = PostgresConnectionString.Replace("[databasename]", $"{environmentLabel}data");
            PostgresConnectionString = PostgresConnectionString.Replace("[databasename]", config.GetValue<string>("Database:Name"));

            RedisConnectionString = config.GetValue<string>("RedisConnectionString");

            TdaAccessToken = config["TdaAccessToken"];
            TdaClientId = config["TdaClientId"];
            TdaApiKey = config["TdaApiKey"];
            TdaRefreshToken = HttpUtility.UrlEncode(config["tdaRefreshToken"]);
            ORATSKey = config["ORATSKey"];
            TradierAPIToken = config["TradierAPIToken"];

            StripePublicKey = config["StripePublicKey"];
            StripeSecretKey = config["StripeSecretKey"];
            StripeSubscriptionPriceId = config["StripeSubscriptionPriceId"];

            Secret = config["JWTSecret"];
        }
        
        public string ProductionVersion(string config)
        {
            var envLabel = EnvironmentLabel();

            if (config.StartsWith(envLabel))
            {
                return config.Replace(envLabel, "");
            }
            else
            {
                return config;
            }
        }

        public string ServiceBusTradesTopic(string ticker)
        {
            return ServiceBusTradesTopicPattern.Replace("{ticker}", ticker);
        }

        private string EnvironmentLabel()
        {
            string environmentLabel = "";
            if (!_env.IsProduction())
            {
                environmentLabel = Environment.GetEnvironmentVariable("AppConfigurationLabel");
                if (environmentLabel == null)
                {
                    environmentLabel = _env.EnvironmentName;
                }
                environmentLabel += "-";
                environmentLabel = environmentLabel.ToLower();
            }
            return environmentLabel;
        }
    }

    public enum Scope
    {
        frontend,
        api
    }

    public static class ChonkyExtensions
    {
        public static IHostBuilder UseChonky(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureAppConfiguration((context, config) =>
            {
                var settings = config.Build();
                var connection = Environment.GetEnvironmentVariable("ConnectionString");
                config.AddAzureAppConfiguration(options =>
                    options
                        .Connect(connection)
                        .Select(KeyFilter.Any, LabelFilter.Null)
                        .Select(KeyFilter.Any, context.HostingEnvironment.EnvironmentName)
                        .Select(KeyFilter.Any, Environment.GetEnvironmentVariable("AppConfigurationLabel")));
            });
            hostBuilder.ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<ChonkyConfiguration>();
            });

            hostBuilder.ConfigureLogging((context, logging) =>
            {
                if (context.HostingEnvironment.EnvironmentName == "Production")
                {
                    IConfigurationSection section = context.Configuration.GetSection("Sentry");

                    logging.Services.Configure<SentryLoggingOptions>(section);
                    logging.AddSentry();
                }
            });

            return hostBuilder;
        }
    }
}
