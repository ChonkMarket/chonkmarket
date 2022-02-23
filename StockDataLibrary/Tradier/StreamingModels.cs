namespace StockDataLibrary.Tradier.StreamingModels
{
    using StockDataLibrary.Models;
    using System;
    using System.Text.Json.Serialization;

    public interface ITradierStreamingData
    {
        public string Symbol { get; set; }
        public long Snaptime { get; }
        public long Date { get; set; }
        public void AddToSnapshot(TradierSnapshot snapshot);
        public TradierSnapshot CreateSnapshot();
    }

    public class Trade : ITradierStreamingData
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; }
        [JsonPropertyName("price")]
        public float Price { get; set; }
        [JsonPropertyName("size")]
        public long Size { get; set; }
        [JsonPropertyName("cvol")]
        public long CumulativeVolume { get; set; }
        [JsonPropertyName("date")]
        public long Date { get; set; }
        public long Snaptime {  get
            {
                var secondsTime = Date / 1000;
                return secondsTime - (secondsTime % 30);
            }
        }

        public void AddToSnapshot(TradierSnapshot snapshot)
        {
            snapshot.TradeCount++;
            snapshot.LocalVolume += Size;
            snapshot.High = Math.Max(snapshot.High, Price);
            snapshot.Low = Math.Min(snapshot.Low, Price);
            snapshot.CumulativeVolume = Math.Max(snapshot.CumulativeVolume, CumulativeVolume);
            if (Date < snapshot.OpenTime)
            {
                snapshot.Open = Price;
                snapshot.OpenTime = Date;
            }
            if (Date > snapshot.CloseTime)
            {
                snapshot.Close = Price;
                snapshot.CloseTime = Date;
            }
        }

        public TradierSnapshot CreateSnapshot()
        {
            return new TradierSnapshot()
            {
                Time = Snaptime,
                OpenTime = Date,
                CloseTime = Date,
                High = Price,
                Low = Price,
                Open = Price,
                Close = Price,
                CumulativeVolume = CumulativeVolume,
                TradeCount = 1,
                LocalVolume = Size,
            };
        }

        // ignored fields, but added here for completeness
        //
        //[JsonPropertyName("type")]
        //public string Type { get; set; }
        //[JsonPropertyName("exch")]
        //public string Exchange { get; set; }
        //[JsonPropertyName("last")]
        //public string LastPrice { get; set; }
    }

    public class Timesale : ITradierStreamingData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("symbol")]
        public string Symbol { get; set; }

        [JsonPropertyName("exch")]
        public string Exchange { get; set; }

        [JsonPropertyName("bid")]
        public float Bid { get; set; }

        [JsonPropertyName("ask")]
        public float Ask { get; set; }

        [JsonPropertyName("last")]
        public float Last { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("date")]
        public long Date { get; set; }
        public long Snaptime
        {
            get
            {
                var secondsTime = Date / 1000;
                return secondsTime - (secondsTime % 30);
            }
        }

        public void AddToSnapshot(TradierSnapshot snapshot)
        {
            snapshot.TimesaleCount++;
            snapshot.LocalTimesaleVolume += Size;
            snapshot.High = Math.Max(snapshot.High, Last);
            snapshot.Low = Math.Min(snapshot.Low, Last);
            if (Date < snapshot.OpenTime)
            {
                snapshot.Open = Last;
                snapshot.OpenTime = Date;
            }
            if (Date > snapshot.CloseTime)
            {
                snapshot.Close = Last;
                snapshot.CloseTime = Date;
            }
        }

        public TradierSnapshot CreateSnapshot()
        {
            return new TradierSnapshot()
            {
                Time = Snaptime,
                OpenTime = Date,
                CloseTime = Date,
                High = Last,
                Low = Last,
                Open = Last,
                Close = Last,
                TimesaleCount = 1,
                LocalTimesaleVolume = Size
            };
        }

        // ignored fields, but added here for completeness
        //
        //[JsonPropertyName("seq")]
        //public long SequenceNumber { get; set; }
        //[JsonPropertyName("flag")]
        //public string ReferenceFlag { get; set; }
        //[JsonPropertyName("cancel")]
        //public bool Cancel { get; set; }
        //[JsonPropertyName("correction")]
        //public bool Correction { get; set; }
        //[JsonPropertyName("session")]
        //public string Session { get; set; }
    }
}
