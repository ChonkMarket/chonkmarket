namespace ChonkyWeb
{
    using ChonkyWeb.Services;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using StockDataLibrary;

    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseChonky()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseSentry();
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<INopeDataService, NopeDataService>();
                    services.AddHostedService<UpdateListener>();
                });
    }
}
