namespace TDAStreamer
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using StockDataLibrary;
    using StockDataLibrary.Services;
    using StockDataLibrary.TDAmeritrade;

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
                    services.AddSingleton<IDataStreamerClient, TDAClient>();
                    services.AddSingleton<ITDAWebSocketClient, TDAWebSocketClient>();
                    services.AddSingleton<IServiceBusProvider, ServiceBusProvider>();
                    services.AddHostedService<Worker>();
                });
    }
}
