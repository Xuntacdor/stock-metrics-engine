using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LoanAmount",
                table: "CashWallets",
                type: "decimal(18,4)",
                nullable: true,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "RiskAlerts",
                columns: table => new
                {
                    AlertID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    AlertType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Rtt = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RiskAlerts", x => x.AlertID);
                    table.ForeignKey(
                        name: "FK_RiskAlerts_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RiskAlerts_CreatedAt",
                table: "RiskAlerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAlerts_UserID",
                table: "RiskAlerts",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RiskAlerts");

            migrationBuilder.DropColumn(
                name: "LoanAmount",
                table: "CashWallets");
        }
    }
}
