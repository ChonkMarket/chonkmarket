namespace StockDataLibraryTests.Models
{
    using StockDataLibrary;
    using StockDataLibrary.Models;
    using System;
    using Xunit;

    public class StockQuoteTests
    {
        [Fact]
        public void IsMarketOpen()
        {
            var quote = new TdaStockQuote();
            var hours = TradingHours.GetMarketHours(DateTime.Now);
            quote.TradeTime = hours.Open - 1;
            Assert.False(quote.IsMarketOpen());
            quote.TradeTime = hours.Close + 1;
            Assert.False(quote.IsMarketOpen());
            quote.TradeTime = hours.Open;
            Assert.True(quote.IsMarketOpen());
            quote.TradeTime = hours.Close;
            Assert.False(quote.IsMarketOpen());
            quote.TradeTime = hours.Close - 1;
            Assert.True(quote.IsMarketOpen());
            quote.TradeTime = 1618407010545;
            Assert.True(quote.IsMarketOpen());
            quote.TradeTime = 1618430386932;
            Assert.True(quote.IsMarketOpen());
        }
    }
}
