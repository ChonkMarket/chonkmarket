namespace TradierDataStreamer
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using StockDataLibrary;
    using StockDataLibrary.Services;
    using StockDataLibrary.Tradier;

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
                    services.AddSingleton<IServiceBusProvider, ServiceBusProvider>();
                    services.AddSingleton<ITradierWebSocketClient, TradierWebSocketClient>();
                    services.AddHostedService<Worker>();
                    // services.AddHostedService<TestWorker>();
                });
    }
}
