using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace StockDataLibrary.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Symbol = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OratsOptionChains",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockId = table.Column<int>(type: "integer", nullable: false),
                    RawData = table.Column<string>(type: "text", nullable: true),
                    TotalPutOptionDelta = table.Column<float>(type: "real", nullable: false),
                    TotalCallOptionDelta = table.Column<float>(type: "real", nullable: false),
                    LocalPutOptionDelta = table.Column<float>(type: "real", nullable: false),
                    LocalCallOptionDelta = table.Column<float>(type: "real", nullable: false),
                    QuoteDate = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OratsOptionChains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OratsOptionChains_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OratsOptionChains_StockId_QuoteDate",
                table: "OratsOptionChains",
                columns: new[] { "StockId", "QuoteDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_Symbol",
                table: "Stocks",
                column: "Symbol",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OratsOptionChains");

            migrationBuilder.DropTable(
                name: "Stocks");
        }
    }
}
