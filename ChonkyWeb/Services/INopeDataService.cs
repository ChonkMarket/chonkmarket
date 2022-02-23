namespace ChonkyWeb.Services
{
    using StockDataLibrary.Models;
    using StockDataLibrary.TDAmeritrade;
    using System.Threading.Tasks;

    public interface INopeDataService
    {
        Task<string> GetJsonAsync(string symbol, long quotetime, string date, MarketHours hours);
        Task UpdateCache(TdaStockQuote quote);
        Task<string> GetJsonNewDataAsync(string symbol, MarketHours hours);
    }
}
