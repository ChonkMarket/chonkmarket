namespace StockDataLibrary.Models
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;


    public class StrikePriceMap : Dictionary<string, Dictionary<string, List<TdaOption>>>
    {
        public float TotalOptionDelta(StrikePriceMap otherChain = null, float assetPrice = 0.0f)
        {
            var callDelta = 0.0F;
            foreach (var i in this)
            {
                foreach (var j in i.Value.Values)
                {
                    foreach (var option in j)
                    {
                        if (option.TotalVolume > 0)
                        {
                            if (double.IsFinite(option.Delta))
                            {
                                callDelta += (option.Delta * option.TotalVolume);
                            }
                        }
                    }
                }
            }
            return callDelta;
        }

        public float CalculateGex()
        {
            var gex = 0.0F;
            foreach (var i in this)
            {
                foreach (var j in i.Value.Values)
                {
                    foreach (var option in j)
                    {
                        if (option.OpenInterest > 0)
                        {
                            if (double.IsFinite(option.Gamma))
                            {
                                gex += (option.Gamma * option.OpenInterest);
                            }
                        }
                    }
                }
            }
            return gex;
        }
    }

    public class TdaOptionChain
    {
        public string Symbol { get; set; }
        public float Interval { get; set; }
        public float InterestRate { get; set; }
        public float UnderlyingPrice { get; set; }
        public int Volatilty { get; set; }
        public float DaysToExpiration { get; set; }
        public int NumberOfContracts { get; set; }
        public TdaStockQuote Underlying { get; set; }
        public StrikePriceMap PutExpDateMap { get; set; }
        public StrikePriceMap CallExpDateMap { get; set; }
        public string RawData { get; set; }
        public float Nope { get; private set; }

        private static readonly Lazy<JsonSerializerOptions> jsonOptions = new(() =>
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
            };
            return options;

        });

        public static async Task<TdaOptionChain> ConstructFromJsonAsync(Stream jsonStream, string rawData = "")
        {
            var optionChain = await JsonSerializer.DeserializeAsync<TdaOptionChain>(jsonStream, jsonOptions.Value);
            if (optionChain.Underlying.Symbol != optionChain.Symbol)
            {
                throw new Exception($"Invalid data, underlying stock symbol is ${optionChain.Underlying.Symbol} but option chain is for ${optionChain.Symbol}");
            }
            optionChain.Underlying.TotalCallOptionDelta = optionChain.CallExpDateMap.TotalOptionDelta(optionChain.PutExpDateMap, optionChain.Underlying.Last);
            optionChain.Underlying.TotalPutOptionDelta = optionChain.PutExpDateMap.TotalOptionDelta(optionChain.CallExpDateMap, optionChain.Underlying.Last);
            optionChain.Underlying.CallGex = optionChain.CallExpDateMap.CalculateGex();
            optionChain.Underlying.PutGex = optionChain.PutExpDateMap.CalculateGex();
            optionChain.Nope = (optionChain.Underlying.TotalCallOptionDelta + optionChain.Underlying.TotalPutOptionDelta) * 10000 / optionChain.Underlying.TotalVolume;
            optionChain.Underlying.Nope = optionChain.Nope;
            optionChain.RawData = rawData;
            return optionChain;
        }
    }

    public class TdaOption
    {
        public string PutCall { get; set; }
        public string Symbol { get; set; }
        public string Description { get; set; }
        public string ExchangeName { get; set; }
        public float Bid { get; set; }
        public float Ask { get; set; }
        public float Last { get; set; }
        public int BidSize { get; set; }
        public int AskSize { get; set; }
        public int LastSize { get; set; }
        public float ClosePrice { get; set; }
        public long QuoteTimeInLong { get; set; }
        public float Volatility { get; set; }
        public float Delta { get; set; }
        public float Gamma { get; set; }
        public float Theta { get; set; }
        public float Vega { get; set; }
        public int OpenInterest { get; set; }
        public float TimeValue { get; set; }
        public float StrikePrice { get; set; }
        public long ExpirationDate { get; set; }
        public long LastTradingDay { get; set; }
        public int TotalVolume { get; set; }

        public bool Call { get { return PutCall == "CALL"; } }
        public bool Put { get { return PutCall == "PUT"; } }
    }
}
