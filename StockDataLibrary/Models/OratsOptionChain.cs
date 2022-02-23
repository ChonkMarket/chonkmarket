namespace StockDataLibrary.Models
{
    using CsvHelper;
    using CsvHelper.Configuration;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Globalization;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;

    [Index(nameof(StockId), nameof(QuoteDate), IsUnique = true)]
    public class OratsOptionChain
    {
        private string _symbol;

        public int Id { get; set; }
        [NotMapped]
        public string Symbol
        {
            get => this.Stock == null ? _symbol : this.Stock.Symbol;
            set => _symbol = value;
        }
        public Stock Stock { get; set; }
        [Required]
        public int StockId { get; set; }
        [NotMapped]
        public List<OratsOption> Options { get; set; }
        public string RawData { get; set; }
        public float TotalPutOptionDelta { get; set; }
        public float TotalCallOptionDelta { get; set; }
        public float LocalPutOptionDelta { get; set; }
        public float LocalCallOptionDelta { get; set; }
        [Required]
        public long QuoteDate { get; set; }
        [Required]
        public long UpdatedAt { get; set; }

        private static readonly CsvConfiguration csvConfig = new(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args =>
            {
                var header = args.Header;
                if (header == "Expiration")
                    return "expirDate";
                if (header == "Symbol")
                    return "ticker";
                return Char.ToLower(args.Header[0]) + args.Header[1..];
            }
        };

        public static OratsOptionChain ConstructFromGzippedOratsCsv(Stream gzipStream, string rawData = "")
        {
            var memoryStream = new MemoryStream();
            using GZipStream decompressionStream = new(gzipStream, CompressionMode.Decompress);
            decompressionStream.CopyTo(memoryStream);
            memoryStream.Seek(0, 0);
            return ConstructFromOratsCsv(memoryStream, rawData);
        }

        public static OratsOptionChain ConstructFromOratsCsv(Stream csvStream, string rawData = "")
        {
            using StreamReader reader = new(csvStream);
            using var csv = new CsvReader(reader, csvConfig);
            var records = csv.GetRecords<OratsOption>();
            var chain = new OratsOptionChain()
            {
                Symbol = records.First().Symbol,
                Options = records.ToList()
            };
            chain.QuoteDate = new DateTimeOffset(chain.Options.Max(c => c.QuoteDate)).ToUnixTimeMilliseconds();
            chain.UpdatedAt = new DateTimeOffset(chain.Options.Max(c => c.UpdatedAt)).ToUnixTimeMilliseconds();
            chain.RawData = rawData;
            chain.GenerateDeltas();
            return chain;
        }

        private void GenerateDeltas()
        {
            foreach (var option in Options)
            {
                TotalCallOptionDelta += option.Delta * option.CallVolume;
                TotalPutOptionDelta += (option.Delta - 1) * option.PutVolume;
            }

        }
    }

    public class OratsOption
    {
        public string Symbol { get; set; }
        public float Strike { get; set; }
        public DateTime Expiration { get; set; }
        public int CallVolume { get; set; }
        public int CallOpenInterest { get; set; }
        public float CallBidPrice { get; set; }
        public float CallValue { get; set; }
        public float CallAskPrice { get; set; }
        public int PutVolume { get; set; }
        public int PutOpenInterest { get; set; }
        public float PutBidPrice { get; set; }
        public float PutValue { get; set; }
        public float PutAskPrice { get; set; }

        public float Delta { get; set; }
        public float Gamma { get; set; }
        public float Theta { get; set; }
        public float Vega { get; set; }

        public DateTime QuoteDate { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
