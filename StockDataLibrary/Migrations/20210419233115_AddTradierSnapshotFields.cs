using Microsoft.EntityFrameworkCore.Migrations;

namespace StockDataLibrary.Migrations
{
    public partial class AddTradierSnapshotFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LocalTimesaleVolume",
                table: "TradierSnapshots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TimesaleCount",
                table: "TradierSnapshots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocalTimesaleVolume",
                table: "TradierSnapshots");

            migrationBuilder.DropColumn(
                name: "TimesaleCount",
                table: "TradierSnapshots");
        }
    }
}
