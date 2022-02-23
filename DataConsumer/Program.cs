namespace DataConsumer
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using StockDataLibrary;
    using StockDataLibrary.Db;
    using StockDataLibrary.Models;
    using StockDataLibrary.TDAmeritrade;
    using System.Collections.Concurrent;

    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseChonky()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<ConcurrentQueue<TdaOptionChain>>();
                    services.AddHostedService<BackgroundQuoteProcessing>();
                    services.AddSingleton<ICass, Cass>();
                    services.AddHostedService<OptionChainConsumer>();
                });
    }
}
