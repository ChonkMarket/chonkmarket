namespace StockDataLibraryTests.Models
{
    using Microsoft.EntityFrameworkCore;
    using StockDataLibrary.Db;
    using StockDataLibrary.Models;
    using System.IO;
    using System.Linq;
    using Xunit;

    public class OratsModelTests
    {
        private readonly DbContextOptions<DataDbContext> dbOptions;
        public OratsModelTests()
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataDbContext>();
            optionsBuilder.UseInMemoryDatabase("MyInMemoryDatabseName");
            dbOptions = optionsBuilder.Options;
        }

        [Fact]
        public void TestProcessingORATSData()
        {
            var filename = "SPY-1618527460-a600f9f6-e36a-4a37-907f-3932fb7d910a.csv";
            var validData = File.OpenRead(Path.Join("Data", "ORATS", filename));
            var validDataChain = OratsOptionChain.ConstructFromOratsCsv(validData, filename);
            Assert.Equal("SPY", validDataChain.Symbol);
            Assert.Equal(5239, validDataChain.Options.Count);
            Assert.Equal(821913.1F, validDataChain.TotalCallOptionDelta);
            Assert.Equal(-554051.06F, validDataChain.TotalPutOptionDelta);
            Assert.Equal(1618517660000, validDataChain.QuoteDate);
            Assert.Equal(1618517666000, validDataChain.UpdatedAt);
            Assert.Equal(filename, validDataChain.RawData);
        }

        [Fact]
        public void TestStoringOptionChains()
        {
            var filename = "SPY-1618527460-a600f9f6-e36a-4a37-907f-3932fb7d910a.csv";
            var validData = File.OpenRead(Path.Join("Data", "ORATS", filename));
            var validDataChain = OratsOptionChain.ConstructFromOratsCsv(validData, filename);
            Assert.Equal(0, validDataChain.Id);
            using (var dbContext = new DataDbContext(dbOptions))
            {
                dbContext.AddOptionChain(validDataChain);
                dbContext.SaveChanges();
            }
            Assert.Equal(1, validDataChain.Id);
            Assert.Equal(1, validDataChain.Stock.Id);
            validData = File.OpenRead(Path.Join("Data", "ORATS", "SPY-1618595360-cd0a406f-6e15-4ca0-9568-1ea8c454f843.csv.gz"));
            validDataChain = OratsOptionChain.ConstructFromGzippedOratsCsv(validData);
            using (var dbContext = new DataDbContext(dbOptions))
            {
                dbContext.AddOptionChain(validDataChain);
                dbContext.SaveChanges();
            }
            Assert.Equal(2, validDataChain.Id);
            Assert.Equal(1, validDataChain.Stock.Id);
        }

        [Fact]
        public void SymbolWorksAfterLoadingFromDatabase()
        {
            var filename = "SPY-1618527460-a600f9f6-e36a-4a37-907f-3932fb7d910a.csv";
            var validData = File.OpenRead(Path.Join("Data", "ORATS", filename));
            var validDataChain = OratsOptionChain.ConstructFromOratsCsv(validData, filename);
            using (var dbContext = new DataDbContext(dbOptions))
            {
                dbContext.AddOptionChain(validDataChain);
                dbContext.SaveChanges();
            }
            Assert.Equal("SPY", validDataChain.Stock.Symbol);
            Assert.Equal("SPY", validDataChain.Symbol);
            using (var dbContext = new DataDbContext(dbOptions))
            {
                var requeried = dbContext.OratsOptionChains.Include(b => b.Stock).FirstOrDefault(o => o.Id == validDataChain.Id);
                Assert.Equal("SPY", requeried.Stock.Symbol);
                Assert.Equal("SPY", requeried.Symbol);
            }
        }

        [Fact]
        public void TestProcessingGzippedORATSData()
        {
            var validData = File.OpenRead(Path.Join("Data", "ORATS", "SPY-1618595360-cd0a406f-6e15-4ca0-9568-1ea8c454f843.csv.gz"));
            var validDataChain = OratsOptionChain.ConstructFromGzippedOratsCsv(validData);
            Assert.Equal("SPY", validDataChain.Symbol);
            Assert.Equal(5294, validDataChain.Options.Count);
            Assert.Equal(545985.6F, validDataChain.TotalCallOptionDelta);
            Assert.Equal(-328119.2F, validDataChain.TotalPutOptionDelta);
            Assert.Equal(1618595343000, validDataChain.QuoteDate);
            Assert.Equal(1618595351000, validDataChain.UpdatedAt);
        }
    }
}
