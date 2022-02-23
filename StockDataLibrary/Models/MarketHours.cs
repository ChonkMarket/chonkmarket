namespace StockDataLibrary.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public struct MarketHours
    {
        public long Open;
        public long Close;
        public string Date;

        public MarketHours(long open, long close, string date = "")
        {
            this.Open = open;
            this.Close = close;
            this.Date = date;
        }
    }
}
