extern alias tz;

namespace ChonkyBot
{
    using Azure.Storage.Queues;
    using ChonkyWeb.Models;
    using ChonkyWeb.Modelsl.V1ApiModels;
    using Discord.WebSocket;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using StockDataLibrary;
    using StockDataLibrary.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using TZConverter = tz::TimeZoneConverter.TZConvert;

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly DiscordSocketClient _client;
        private readonly string azureBlobKeyConnectionString;
        private bool connected = false;
        private readonly string token;
        private readonly string processedChainResultQueueName;
        private readonly string userUpdateQueueName;
        private readonly JsonSerializerOptions options = new();
        private QueueClient processedChainResultQueueClient;
        private QueueClient userUpdateQueueClient;
        private readonly TimeZoneInfo estTimeZone;
        private readonly Dictionary<string, SocketTextChannel> channels = new();
        private SocketCategoryChannel categoryChannel;
        private SocketCategoryChannel adminChannel;
        private SocketGuild guild;
        private readonly TaskCompletionSource ready = new();
        private readonly ChonkyConfiguration _chonkyConfiguration;

        public Worker(ILogger<Worker> logger, ChonkyConfiguration config)
        {
            _logger = logger;
            _client = new DiscordSocketClient();
            processedChainResultQueueName = config.ProcessedChainResultQueueName;
            userUpdateQueueName = config.UserUpdateQueueName;
            azureBlobKeyConnectionString = config.AzureBlobKeyConnectionString;
            token = config.DiscordBotToken;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
            estTimeZone = TZConverter.GetTimeZoneInfo("America/New_York");
            _chonkyConfiguration = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _client.LoginAsync(Discord.TokenType.Bot, token);
            await _client.StartAsync();

            processedChainResultQueueClient = new QueueClient(azureBlobKeyConnectionString, processedChainResultQueueName);
            await processedChainResultQueueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

            userUpdateQueueClient = new QueueClient(azureBlobKeyConnectionString, userUpdateQueueName);
            await userUpdateQueueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

            _client.Ready += () =>
            {
                // hardcoded 15b
                guild = _client.GetGuild(822477539011723334);
                categoryChannel = guild.CategoryChannels.Where(pred => { return pred.Name == "tickers"; }).First();
                adminChannel = guild.CategoryChannels.Where(pred => { return pred.Name == "admin"; }).First();
                if (!_chonkyConfiguration.IsProduction)
                    categoryChannel = adminChannel;
                connected = true;
                _logger.LogInformation("Bot is connected!");
                ready.SetResult();
                return Task.CompletedTask;
            };

            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await ready.Task;
            _ = TickerLoop(stoppingToken);
            _ = UserUpdateLoop(stoppingToken);
        }

        private async Task TickerLoop(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var response = (await processedChainResultQueueClient.ReceiveMessageAsync(TimeSpan.FromSeconds(30), stoppingToken)).Value;
                if (response != null)
                {
                    var message = response.Body.ToString();
                    try
                    {
                        await ProcessMessage(message);
                        await processedChainResultQueueClient.DeleteMessageAsync(response.MessageId, response.PopReceipt, stoppingToken);
                        await Task.Delay(100, stoppingToken);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Failed to process message: ID: {response.MessageId} - {message}");
                        _logger.LogError(e.Message);
                    }
                }
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        private async Task UserUpdateLoop(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var response = (await userUpdateQueueClient.ReceiveMessageAsync(TimeSpan.FromSeconds(30), stoppingToken)).Value;
                if (response != null)
                {
                    var message = response.Body.ToString();
                    try
                    {
                        var account = JsonSerializer.Deserialize<V1Response<Account>>(message).Data;
                        if (connected)
                        {
                            var channel = await GetOrCreateChannelAsync("user-updates", adminChannel);
                            var outputText = $"New User: {account.Name}#{account.DiscordNameIdentifier}";
                            var embedBuilder = new Discord.EmbedBuilder()
                            {
                                ThumbnailUrl = account.AvatarUrl,
                                Title = $"New User: {account.Name}#{account.DiscordNameIdentifier}",
                                Description = $"User ID: {account.Id}\n {DateTime.Now:MMM dd h:mm tt}"
                            };
                            await channel.SendMessageAsync(embed: embedBuilder.Build());
                            await userUpdateQueueClient.DeleteMessageAsync(response.MessageId, response.PopReceipt, stoppingToken);
                        }
                        await Task.Delay(100, stoppingToken);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Failed to process message: ID: {response.MessageId} - {message}");
                        _logger.LogError(e.Message);
                    }
                }
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }

        }
        private async Task<SocketTextChannel> GetOrCreateChannelAsync(string channelName, SocketCategoryChannel channelCategory)
        {
            if (!channels.TryGetValue(channelName, out SocketTextChannel channel))
            {
                channel = channelCategory.Channels.Where(p => p.Name == channelName.ToLower()).FirstOrDefault() as SocketTextChannel;
                if (channel == null)
                {
                    var restChannel = await guild.CreateTextChannelAsync(channelName, p => p.CategoryId = channelCategory.Id);
                    channel = guild.TextChannels.Where(p => p.Id == restChannel.Id).First();
                }
                channels[channelName] = channel;
            }
            return channel;
        }

        private async Task ProcessMessage(string message)
        {
            TdaStockQuote quote = JsonSerializer.Deserialize<TdaStockQuote>(message, options);
            // don't send messages to the channels if the market wasn't open for the trade time
            //
            if (connected && quote.IsMarketOpen())
            {
                var channel = await GetOrCreateChannelAsync(quote.Symbol, categoryChannel);
                var time = TimeZoneInfo.ConvertTimeFromUtc(DateTimeOffset.FromUnixTimeMilliseconds(quote.TradeTime).UtcDateTime, estTimeZone);
                // nope:  -11.12 | price:  $336.60 | Apr 14  7:59 PM
                //
                string nopeText = $"`nope: {($"{quote.Nope:0.00}"), 7} | price: {($"${quote.Mark:0.00}"), 8} | {time:MMM dd} {($"{time:h:mm tt}"), 8}`";
                await channel.SendMessageAsync(nopeText);
            }
        }

    }
}
