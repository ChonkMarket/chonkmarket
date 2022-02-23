using Microsoft.EntityFrameworkCore.Migrations;

namespace StockDataLibrary.Migrations
{
    public partial class AddTradierSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TradierSnapshots",
                columns: table => new
                {
                    Time = table.Column<long>(type: "bigint", nullable: false),
                    SymbolId = table.Column<int>(type: "integer", nullable: false),
                    OpenTime = table.Column<long>(type: "bigint", nullable: false),
                    CloseTime = table.Column<long>(type: "bigint", nullable: false),
                    High = table.Column<float>(type: "real", nullable: false),
                    Low = table.Column<float>(type: "real", nullable: false),
                    Open = table.Column<float>(type: "real", nullable: false),
                    Close = table.Column<float>(type: "real", nullable: false),
                    LocalVolume = table.Column<long>(type: "bigint", nullable: false),
                    CumulativeVolume = table.Column<long>(type: "bigint", nullable: false),
                    TradeCount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradierSnapshots", x => new { x.Time, x.SymbolId });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradierSnapshots");
        }
    }
}
