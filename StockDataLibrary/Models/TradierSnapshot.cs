namespace StockDataLibrary.Models
{
    using StockDataLibrary.Tradier.StreamingModels;
    using System;

    public class TradierSnapshot
    {
        public long Time { get; set; }
        public int SymbolId { get; set; }
        public long OpenTime { get; set; }
        public long CloseTime { get; set; }
        public float High { get; set; }
        public float Low { get; set; }
        public float Open { get; set; }
        public float Close { get; set; }
        public long LocalVolume { get; set; }
        public long LocalTimesaleVolume { get; set; }
        public long CumulativeVolume { get; set; }
        public long TradeCount { get; set; }
        public long TimesaleCount { get; set; }

        public TradierSnapshot() { }
    }
}
