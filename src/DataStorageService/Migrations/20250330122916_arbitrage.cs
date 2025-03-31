using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataStorageService.Migrations
{
    /// <inheritdoc />
    public partial class arbitrage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArbitrageData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstSymbol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SecondSymbol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TimeFrame = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FirstPrice = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    SecondPrice = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    Spread = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    PercentageSpread = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArbitrageData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArbitrageData_FirstSymbol_SecondSymbol_TimeFrame_Timestamp",
                table: "ArbitrageData",
                columns: new[] { "FirstSymbol", "SecondSymbol", "TimeFrame", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArbitrageData");
        }
    }
}
