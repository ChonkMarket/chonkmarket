
namespace ChonkyWeb
{
    using ChonkyWeb.Services;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Sentry.AspNetCore;
    using StackExchange.Redis;
    using StockDataLibrary;
    using StockDataLibrary.Db;
    using StockDataLibrary.Services;
    using ChonkyWeb.Models;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using System;
    using ChonkyWeb.Middleware;
    using Microsoft.IdentityModel.Tokens;
    using System.Text;
    using System.Threading.Tasks;
    using ChonkyWeb.Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using System.Runtime.InteropServices;
    using Azure.Storage.Blobs;
    using Microsoft.AspNetCore.DataProtection;

    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IHostEnvironment Environment { get; }
        public Startup(IConfiguration configuration, IHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // can't inject the chonky configuration service into startup because of chicken/egg problem
            // so for web hosts, we need to create one here instead
            var config = new ChonkyConfiguration(Configuration, Environment);
            var connectionString = config.PostgresConnectionString;

            // Database Context
            services.AddDbContext<AccountDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddDbContext<DataDbContext>(options =>
            options.UseNpgsql(config.DataPostgresConnectionString));
            services.AddSingleton<IDbContextFactory<DataDbContext>, DataDbContextFactory>();

            // Redis
            services.AddStackExchangeRedisCache(options =>
            {
                ConfigurationOptions configOptions = ConfigurationOptions.Parse(config.RedisConnectionString);
                configOptions.SyncTimeout = 1000;
                options.ConfigurationOptions = configOptions;
            });

            // controller needs
            services.AddSingleton<SSEClientManager>();
            services.AddSingleton<WsSocketManager>();
            services.AddSingleton<IMemoryCache, MemoryCache>();
            services.AddSingleton<IServiceBusProvider, ServiceBusProvider>();

            // Cassandra
            services.AddSingleton<ICass, Cass>();

            // Capture Database related errors and display a friendly error message
            // Only works in development mode
            services.AddDatabaseDeveloperPageExceptionFilter();
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddSingleton<IAuthorizationPolicyProvider, ScopeAuthorizationProvider>();
            services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();
            services.AddSingleton<INopeDataService, NopeDataService>();
            services.AddTransient<TestDataSenderFactory>();

            services.AddAuthentication(options =>
            {
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Events.OnRedirectToAccessDenied =
                  options.Events.OnRedirectToLogin = c =>
                  {
                      c.Response.StatusCode = StatusCodes.Status401Unauthorized;
                      return Task.FromResult<object>(null);
                  };
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(config.Secret)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                };
            })
            .AddDiscord(options =>
            {
                options.ClientId = config.DiscordClientId;
                options.ClientSecret = config.DiscordClientSecret;
                options.CallbackPath = "/signin-discord";
                options.Scope.Add("email");
            });

            // Big helpers that I didn't look at much to see what they do
            services.AddControllers();
            services.AddCustomSwaggerGen(Environment);
            services.AddAutoMapper(typeof(Startup));

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });
            if (Environment.IsDevelopment() && RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { 
                services.Configure<KestrelServerOptions>(options =>
                {
                    options.ListenAnyIP(44396, listenOptions =>
                    {
                        listenOptions.UseHttps("https.pfx", "pass");
                    });
                    options.ListenAnyIP(64147);
                });
            }

            BlobContainerClient container = new(config.AzureBlobKeyConnectionString, "data-protection-key-container");
            var containerInfo = container.CreateIfNotExists();
            var blobClient = container.GetBlobClient("keys.xml");
            services.AddDataProtection().PersistKeysToAzureBlobStorage(blobClient);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseSentryTracing();
                app.UseExceptionHandler("/Error");

                // https://stackoverflow.com/questions/31276849/asp-net-5-oauth-redirect-uri-not-using-https
                // i think our janky setup of front door -> app gateway -> container means that by the time it
                // hits the container, the last x-forwarded-for was http also, so just gonna have to force this in prod
                //
                app.Use(next => context =>
                {
                    context.Request.Scheme = "https";

                    return next(context);
                });
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                if (env.IsDevelopment())
                {
                    c.SwaggerEndpoint("/swagger/internal/swagger.json", "Internal");
                }
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChonkMarket");
            });
            app.UseStaticFiles();
            app.UseSpaStaticFiles();


            app.UseRouting();
            app.UseAuthentication();
            app.UseMiddleware<JwtMiddleware>();
            app.UseAuthorization();

            app.UseWebSockets();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });

            // Hack to work around an exception that the spa middleware will throw
            // when anything but a GET is processed by it
            app.UseWhen(ctx => HttpMethods.IsGet(ctx.Request.Method), builder =>
            {
                builder.UseSpa(spa =>
                {
                    spa.Options.SourcePath = "ClientApp";
                    if (env.IsDevelopment())
                    {
                        spa.UseReactDevelopmentServer(npmScript: "start");
                    }
                });
            });
        }
    }
}
