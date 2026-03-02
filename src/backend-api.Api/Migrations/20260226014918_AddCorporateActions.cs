using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCorporateActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CorporateActions",
                columns: table => new
                {
                    ActionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    ActionType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    RecordDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ratio = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    ProcessedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CorporateActions", x => x.ActionID);
                    table.ForeignKey(
                        name: "FK_CorporateActions_Symbols",
                        column: x => x.Symbol,
                        principalTable: "Symbols",
                        principalColumn: "Symbol");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorporateActions_PaymentDate_Status",
                table: "CorporateActions",
                columns: new[] { "PaymentDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CorporateActions_Symbol",
                table: "CorporateActions",
                column: "Symbol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorporateActions");
        }
    }
}
