namespace ChonkyAdmin
{
    using Microsoft.Extensions.Logging;
    using StockDataLibrary;
    using StockDataLibrary.Db;
    using System;
    using System.Threading.Tasks;

    class VwapReprocessor
    {
        private readonly ICass _cass;
        public VwapReprocessor(ChonkyConfiguration config, ILogger<Cass> logger)
        {
            _cass = new Cass(config, logger);
        }

        public async Task RunAsync(string symbol, string day)
        {
            var marketHours = TradingHours.GetMarketHours(day);
            var quotes = await _cass.FetchQuotesAsync(symbol, marketHours);

            long lastTotalVolume = 0;
            double pv = 0;

            foreach (var quote in quotes)
            {
                var updatedQuote = VwapCalculator.UpdateVwap(quote, lastTotalVolume, pv);
                lastTotalVolume = updatedQuote.TotalVolume;
                await _cass.StoreQuoteAsync(updatedQuote);
            }
        }
    }
}
