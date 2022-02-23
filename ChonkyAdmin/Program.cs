namespace ChonkyAdmin
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using StockDataLibrary;
    using StockDataLibrary.Db;
    using System.Threading.Tasks;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var config = host.Services.GetService<ChonkyConfiguration>();
            var cassLogger = host.Services.GetService<ILogger<Cass>>();

            // blob tagger will go through all the various tickers and retag the raw data
            // var blobTagger = new BlobTagger(config);
            // blobTagger.Run();

            // restreamer will go through the blobs in the production container for a given ticker and date
            // and generate messages in the local environment's queue to be reprocessed by a dataconsumer
            // by default it does the last day of trading
            // var restreamer = new Restreamer(config);
            // restreamer.Run("GME", TradingHours.GetMarketHours(DateTime.Now).Date);

            // vwap reprocessor will go through a days worth of recorded quotes and regenerate the VWAP data
            // for each of the quotes
            // var vwapReprocessor = new VwapReprocessor(config, cassLogger);
            // await vwapReprocessor.RunAsync("SPY", TradingHours.GetMarketHours(DateTime.Now).Date);
            var cass = new Cass(config, cassLogger);
            var cnopeReprocessor = new CopeReprocessor(config, cass, host.Services.GetService<IDbContextFactory<DataDbContext>>());
            await cnopeReprocessor.RunAsync("SPY", "5/14/2021");
            // await cnopeReprocessor.RunOratsAsync("SPY");
            // cnopeReprocessor = new CopeReprocessor(config, cass);
            // await cnopeReprocessor.RunOratsAsync("QQQ");
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseChonky()
            .ConfigureServices((hostContext, services) =>
            {
                var config = new ChonkyConfiguration(hostContext.Configuration, hostContext.HostingEnvironment);
                var connectionString = config.DataPostgresConnectionString;

                services.AddSingleton<IDbContextFactory<DataDbContext>, DataDbContextFactory>();
                services.AddDbContext<DataDbContext>(options =>
                    options.UseNpgsql(connectionString));
            });
    }
}
