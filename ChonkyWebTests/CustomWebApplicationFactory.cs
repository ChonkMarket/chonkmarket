namespace ChonkyWebTests
{
    using ChonkyWeb.Models;
    using ChonkyWeb.Services;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Moq;
    using StockDataLibrary;
    using StockDataLibrary.Db;
    using System;
    using System.Linq;

    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        public Mock<ICass> CassMock { get; set; } = new Mock<ICass>();
        private bool _databaseInitialized = false;
        public Account User { get; private set; }
        public ChonkyConfiguration ChonkyConfiguration { get; private set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AccountDbContext>));
                services.Remove(descriptor);
                descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(Cass));
                services.Remove(descriptor);

                // this background listener guy really sucks in test mode
                // just deadlocks the webserver
                // probably because i'm doing something bad in there that i shouldn't be doing
                //
                descriptor = services.SingleOrDefault(d => d.ImplementationType == typeof(UpdateListener));
                services.Remove(descriptor);

                services.AddSingleton<ICass>(CassMock.Object);

                services.AddDbContext<AccountDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });

                var sp = services.BuildServiceProvider();

                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AccountDbContext>();
                ChonkyConfiguration = scopedServices.GetRequiredService<ChonkyConfiguration>();
                var logger = scopedServices
                    .GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

                db.Database.EnsureCreated();

                try
                {
                    SeedDb(db);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred seeding the " +
                        "database with test messages. Error: {Message}", ex.Message);
                }
            });

            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.Test.json", optional: false);
            });
        }

        public AccountDbContext GenerateDbContext()
        {
            var options = new DbContextOptionsBuilder<AccountDbContext>()
                .UseInMemoryDatabase("InMemoryDbForTesting")
                .Options;
            return new AccountDbContext(options);
        }

        private void SeedDb(AccountDbContext db)
        {
            if (!_databaseInitialized)
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var account = new Account();
                User = account;
                account.Id = 1;
                account.Role = Role.Subscriber;
                account.StripeCustomerId = "cus_JFnWajgPJMqaKo";
                db.Add(account);
                db.SaveChanges();
            }

            _databaseInitialized = true;
        }
    }
}
