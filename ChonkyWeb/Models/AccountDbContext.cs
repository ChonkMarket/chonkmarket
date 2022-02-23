namespace ChonkyWeb.Models
{
    using Microsoft.EntityFrameworkCore;
    using StockDataLibrary;

    public class AccountDbContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }

        public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options)
        {
        }
    }
}
