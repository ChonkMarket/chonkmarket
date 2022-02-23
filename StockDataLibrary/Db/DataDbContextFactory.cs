namespace StockDataLibrary.Db
{
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class DataDbContextFactory : IDbContextFactory<DataDbContext>
    {
        private readonly ChonkyConfiguration _chonkyConfiguration;

        public DataDbContextFactory() { }
        public DataDbContextFactory(ChonkyConfiguration chonkyConfiguration)
        {
            _chonkyConfiguration = chonkyConfiguration;
        }

        public DataDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<DataDbContext>();
            options.UseNpgsql(_chonkyConfiguration.DataPostgresConnectionString);
            return new DataDbContext(options.Options);
        }
    }
}
