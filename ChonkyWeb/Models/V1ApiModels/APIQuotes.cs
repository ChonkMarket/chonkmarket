namespace ChonkyWeb.Models.V1ApiModels
{
    using System.Collections.Generic;

    public class APIQuotes
    {
        public QuoteInfo Info { get; set; }
        public List<Quote> Quotes { get; set; }
    }

    public class QuoteInfo
    {
        public string Symbol { get; set; }
        public string Description { get; set; }
    }

    public class Quote
    {
        public float High { get; set; }
        public float Low { get; set; }
        public float Open { get; set; }
        public float Close { get; set; }
        public float Mark { get; set; }
        public long QuoteTime { get; set; }
        public long TotalVolume { get; set; }
        public float Nope { get; set; }
        public float TotalPutOptionDelta { get; set; }
        public float TotalCallOptionDelta { get; set; }
        public float LocalPutOptionDelta { get; set; }
        public float LocalCallOptionDelta { get; set; }
        public long LocalVolume { get; set; }
    }
}
