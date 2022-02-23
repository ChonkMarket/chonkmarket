namespace ChonkyAdmin
{
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using StockDataLibrary;
    using StockDataLibrary.Db;
    using StockDataLibrary.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    class CopeReprocessor
    {
        private readonly ICass _cass;
        private readonly string azureBlobKeyConnectionString;
        private readonly ChonkyConfiguration _config;
        private readonly CopeCalculator _calc;
        private readonly IDbContextFactory<DataDbContext> _dbContextFactory;
        private readonly Dictionary<string, int> symbolCache = new();

        public CopeReprocessor(ChonkyConfiguration config, Cass cass, IDbContextFactory<DataDbContext> dbDataContextFactory)
        {
            azureBlobKeyConnectionString = config.AzureBlobKeyConnectionString;
            _config = config;
            _cass = cass;
            _calc = new CopeCalculator();
            _dbContextFactory = dbDataContextFactory;
        }

        public Task RunAsync(string symbol, long quoteTime)
        {
            var hours = TradingHours.GetMarketHours(quoteTime);
            hours.Open = quoteTime;
            return RunAsync(symbol, hours);
        }
        public Task RunAsync(string symbol, string day)
        {
            var hours = TradingHours.GetMarketHours(day);
            hours.Open -= 60000;
            return RunAsync(symbol, hours);
        }
        public Task RunAsync(string symbol)
        {
            return RunAsync(symbol, TradingHours.GetMarketHours(DateTime.Now));
        }

        public async Task RunOratsAsync(string symbol)
        {
            MarketHours hours;
            var containerName = (_config.OratsContainerName + $"-{symbol}").ToLower();
            var container = new BlobContainerClient(azureBlobKeyConnectionString, containerName);
            var blobs = container.GetBlobs();

            foreach (var blob in blobs)
            {
                var split = blob.Name.Split("/");
                var splitName = split[1].Split("-");
                var time = Convert.ToInt64(splitName[1]) * 1000;
                hours = TradingHours.GetMarketHours(time);
                if (time > hours.Close || time < hours.Open - 30000)
                    continue;
                Console.WriteLine(blob.Name);
                var jsonBlob = container.GetBlobClient(blob.Name);
                BlobDownloadInfo response = await jsonBlob.DownloadAsync();

                if (response.ContentLength < 500)
                    continue;

                try
                {
                    OratsOptionChain chain;
                    if (blob.Name.Contains("gz"))
                        chain = OratsOptionChain.ConstructFromGzippedOratsCsv(response.Content);
                    else
                        chain = OratsOptionChain.ConstructFromOratsCsv(response.Content);
                    chain.RawData = blob.Name;
                    await ProcessChainAsync(chain);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
            }
        }

        private async Task ProcessChainAsync(OratsOptionChain chain)
        {
            _calc.Calculate(chain);
            using var dbContext = _dbContextFactory.CreateDbContext();
                if (symbolCache.TryGetValue(chain.Symbol, out var id))
                {
                    chain.StockId = id;
                }
                else
                {
                    var stock = dbContext.FindOrCreateStock(chain.Symbol);
                    symbolCache[chain.Symbol] = stock.Id;
                    chain.StockId = stock.Id;
                }
            await dbContext.OratsOptionChains
                .Upsert(chain)
                .On(v => new { v.StockId, v.QuoteDate })
                .WhenMatched(v => new OratsOptionChain
                {
                    LocalCallOptionDelta = v.LocalCallOptionDelta,
                    LocalPutOptionDelta = v.LocalPutOptionDelta,
                    TotalCallOptionDelta = v.TotalCallOptionDelta,
                    TotalPutOptionDelta = v.TotalPutOptionDelta
                })
                .RunAsync();

        }

        public async Task RunAsync(string symbol, MarketHours hours)
        {
            var containerName = (_config.ContainerName + $"-{symbol}").ToLower();
            var container = new BlobContainerClient(azureBlobKeyConnectionString, containerName);
            var blobs = container.GetBlobs();
            var date = "";

            foreach (var blob in blobs)
            {
                var splitName = blob.Name.Split("-");
                var time = Convert.ToInt64(splitName[1]) * 1000;
                // hours = TradingHours.GetMarketHours(time);
                if (time > hours.Close || time < hours.Open - 30000)
                    continue;
                if (date != hours.Date)
                {
                    var jsonBlob = container.GetBlobClient(blob.Name);
                    BlobDownloadInfo response = await jsonBlob.DownloadAsync();

                    if (response.ContentLength < 500)
                        continue;

                    try
                    {
                        var chain = await TdaOptionChain.ConstructFromJsonAsync(response.Content);
                        chain.RawData = blob.Name;
                        try
                        {
                            var underlying = await _cass.FetchQuoteAsync(symbol, chain.Underlying.QuoteTime);
                            chain.Underlying = underlying;
                        }
                        catch (InvalidOperationException e)
                        {
                            Console.WriteLine(e.Message);
                            _calc.Calculate(chain);
                            Console.WriteLine($"{chain.Underlying.Nope}");
                            await _cass.StoreQuoteAsync(chain);
                        }
                        _calc.SetPrevious(chain);
                        date = hours.Date;
                    } catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                else
                {
                    Console.WriteLine(blob.Name);
                    var jsonBlob = container.GetBlobClient(blob.Name);
                    BlobDownloadInfo response = await jsonBlob.DownloadAsync();

                    if (response.ContentLength < 500)
                        continue;

                    TdaOptionChain chain;
                    try
                    {
                        chain = await TdaOptionChain.ConstructFromJsonAsync(response.Content);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        continue;
                    }
                    chain.RawData = blob.Name;
                    _calc.Calculate(chain);
                    Console.WriteLine($"{chain.Underlying.Nope}");
                    await _cass.StoreQuoteAsync(chain);
                }
            }
        }
    }
}
