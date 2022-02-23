namespace StockDataLibrary
{
    using StockDataLibrary.Models;
    using StockDataLibrary.TDAmeritrade;

    public static class VwapCalculator
    {
        public static TdaStockQuote UpdateVwap(TdaStockQuote quote, long lastTotalVolume, double previousPv)
        {
            quote.LocalVolume = quote.TotalVolume - lastTotalVolume;
            double localPv = quote.Mark * quote.LocalVolume;
            return quote;
        }
    }
}
