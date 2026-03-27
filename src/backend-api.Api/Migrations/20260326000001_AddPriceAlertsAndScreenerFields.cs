using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceAlertsAndScreenerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── PriceAlerts table ─────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "PriceAlerts",
                columns: table => new
                {
                    AlertId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Symbol = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    AlertType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Condition = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    ThresholdValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsTriggered = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    NotifyOnce = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getutcdate())"),
                    TriggeredAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceAlerts", x => x.AlertId);
                    table.ForeignKey(
                        name: "FK_PriceAlerts_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_UserID",
                table: "PriceAlerts",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_Symbol_Active",
                table: "PriceAlerts",
                columns: new[] { "Symbol", "IsActive" });

            // ── Screener columns on Symbols ───────────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "Symbols",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Pe",
                table: "Symbols",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MarketCap",
                table: "Symbols",
                type: "decimal(20,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PriceAlerts");

            migrationBuilder.DropColumn(name: "Sector", table: "Symbols");
            migrationBuilder.DropColumn(name: "Pe", table: "Symbols");
            migrationBuilder.DropColumn(name: "MarketCap", table: "Symbols");
        }
    }
}
