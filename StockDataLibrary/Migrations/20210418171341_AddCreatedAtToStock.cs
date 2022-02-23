using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StockDataLibrary.Migrations
{
    public partial class AddCreatedAtToStock : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Stocks",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "now()");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Stocks");
        }
    }
}
