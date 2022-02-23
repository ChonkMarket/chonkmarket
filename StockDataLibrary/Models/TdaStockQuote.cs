namespace StockDataLibrary.Models
{
    using Cassandra.Mapping;
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class TdaQuoteMappings : Mappings
    {
        public TdaQuoteMappings()
        {
            For<TdaStockQuote>()
                .TableName(TdaStockQuote.CASS_TABLE_NAME)
                .PartitionKey("Symbol")
                .ClusteringKey("QuoteTime");
        }
    }

    public class TdaStockQuote
    {
        public const string CASS_TABLE_NAME = "newquotes";
        public string Symbol { get; set; }
        public float Change { get; set; }
        public float PercentChange { get; set; }
        public float Close { get; set; }
        public long QuoteTime { get; set; }
        public long TradeTime { get; set; }
        public float Bid { get; set; }
        public float Ask { get; set; }
        public float Last { get; set; }
        public float Mark { get; set; }
        public float MarkChange { get; set; }
        public float MarkPercentChange { get; set; }
        public int BidSize { get; set; }
        public int AskSize { get; set; }
        public float HighPrice { get; set; }
        public float LowPrice { get; set; }
        public float OpenPrice { get; set; }
        public long TotalVolume { get; set; }
        public float TotalPutOptionDelta { get; set; }
        public float TotalCallOptionDelta { get; set; }
        public float LocalPutOptionDelta { get; set; }
        public float LocalCallOptionDelta { get; set; }
        public float Nope { get; set; }
        public float CallGex { get; set; }
        public float PutGex { get; set; }
        public string RawData { get; set; }
        public long LocalVolume { get; set; }

        public bool IsMarketOpen()
        {
            return TradingHours.IsMarketOpen(TradeTime);
        }
    }
}
