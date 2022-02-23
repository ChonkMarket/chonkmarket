namespace OratsTradierDataConsumer
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using StockDataLibrary;
    using StockDataLibrary.Db;

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
                    var config = new ChonkyConfiguration(hostContext.Configuration, hostContext.HostingEnvironment);
                    var connectionString = config.DataPostgresConnectionString;

                    services.AddSingleton<IDbContextFactory<DataDbContext>, DataDbContextFactory>();
                    services.AddHostedService<OratsOptionChainConsumer>();
                    services.AddHostedService<TradierDataConsumer>();
                    // Database Context
                    // services.AddHostedService<TradierDataTester>();
                    services.AddDbContext<DataDbContext>(options =>
                        options.UseNpgsql(connectionString));
                });
    }
}
