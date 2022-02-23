namespace StockDataLibrary.Db
{
    using Microsoft.EntityFrameworkCore;
    using StockDataLibrary.Models;
    using System.Collections.Generic;
    using System.Linq;

    public class DataDbContext : DbContext
    {
        private static readonly object _locker = new();
        public DbSet<OratsOptionChain> OratsOptionChains { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<TradierSnapshot> TradierSnapshots { get; set; }

        public DataDbContext() { }

        public DataDbContext(DbContextOptions<DataDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TradierSnapshot>().HasKey(c => new { c.Time, c.SymbolId });
        }

        public Stock FindOrCreateStock(string symbol)
        {
            symbol = symbol.ToUpper();
            var stock = Stocks.Where(x => x.Symbol == symbol).FirstOrDefault();
            if (stock == null)
            {
                stock = new Stock() { Symbol = symbol };
                Stocks.Add(stock);
                SaveChanges();
            }
            return stock;
        }

        public void AddOptionChain(OratsOptionChain chain)
        {
            if (chain.StockId == 0 || chain.Stock == null)
            {
                chain.Stock = FindOrCreateStock(chain.Symbol);
                chain.StockId = chain.Stock.Id;
            }
            OratsOptionChains.Add(chain);
        }
    }
}
