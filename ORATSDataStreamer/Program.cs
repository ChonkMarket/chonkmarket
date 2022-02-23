namespace OratsDataStreamer
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using StockDataLibrary;
    using StockDataLibrary.Orats;
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
                    services.AddSingleton<IDataStreamerClient, ApiClient>();
                    services.AddHostedService<Worker>();
                });
    }
}
