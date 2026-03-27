using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeModelPropertiesNonNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MarketCap",
                table: "Symbols",
                type: "decimal(20,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Pe",
                table: "Symbols",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "Symbols",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TotalQuantity",
                table: "Portfolios",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "LockedQuantity",
                table: "Portfolios",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "AvgCostPrice",
                table: "Portfolios",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldNullable: true,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Orders",
                type: "datetime",
                nullable: false,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true,
                oldDefaultValueSql: "(getdate())");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Orders",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "PENDING",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldNullable: true,
                oldDefaultValue: "PENDING");

            migrationBuilder.AlterColumn<int>(
                name: "MatchedQty",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Orders",
                type: "datetime",
                nullable: false,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true,
                oldDefaultValueSql: "(getdate())");

            migrationBuilder.AlterColumn<decimal>(
                name: "LockedAmount",
                table: "CashWallets",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldNullable: true,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "LoanAmount",
                table: "CashWallets",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldNullable: true,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "CashWallets",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldNullable: true,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "AvailableQuantity",
                table: "Portfolios",
                type: "int",
                nullable: false,
                computedColumnSql: "([TotalQuantity]-[LockedQuantity])",
                stored: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldComputedColumnSql: "([TotalQuantity]-[LockedQuantity])",
                oldStored: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AvailableBalance",
                table: "CashWallets",
                type: "decimal(19,4)",
                nullable: false,
                computedColumnSql: "([Balance]-[LockedAmount])",
                stored: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(19,4)",
                oldNullable: true,
                oldComputedColumnSql: "([Balance]-[LockedAmount])",
                oldStored: true);

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
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsTriggered = table.Column<bool>(type: "bit", nullable: false),
                    NotifyOnce = table.Column<bool>(type: "bit", nullable: false),
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
                name: "IX_PriceAlerts_Symbol_Active",
                table: "PriceAlerts",
                columns: new[] { "Symbol", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_UserID",
                table: "PriceAlerts",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceAlerts");

            migrationBuilder.DropColumn(
                name: "MarketCap",
                table: "Symbols");

            migrationBuilder.DropColumn(
                name: "Pe",
                table: "Symbols");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "Symbols");

            migrationBuilder.AlterColumn<int>(
                name: "TotalQuantity",
                table: "Portfolios",
                type: "int",
                nullable: true,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "LockedQuantity",
                table: "Portfolios",
                type: "int",
                nullable: true,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "AvgCostPrice",
                table: "Portfolios",
                type: "decimal(18,4)",
                nullable: true,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Orders",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldDefaultValueSql: "(getdate())");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Orders",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true,
                defaultValue: "PENDING",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldDefaultValue: "PENDING");

            migrationBuilder.AlterColumn<int>(
                name: "MatchedQty",
                table: "Orders",
                type: "int",
                nullable: true,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Orders",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldDefaultValueSql: "(getdate())");

            migrationBuilder.AlterColumn<decimal>(
                name: "LockedAmount",
                table: "CashWallets",
                type: "decimal(18,4)",
                nullable: true,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "LoanAmount",
                table: "CashWallets",
                type: "decimal(18,4)",
                nullable: true,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "CashWallets",
                type: "decimal(18,4)",
                nullable: true,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "AvailableQuantity",
                table: "Portfolios",
                type: "int",
                nullable: true,
                computedColumnSql: "([TotalQuantity]-[LockedQuantity])",
                stored: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldComputedColumnSql: "([TotalQuantity]-[LockedQuantity])",
                oldStored: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AvailableBalance",
                table: "CashWallets",
                type: "decimal(19,4)",
                nullable: true,
                computedColumnSql: "([Balance]-[LockedAmount])",
                stored: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(19,4)",
                oldComputedColumnSql: "([Balance]-[LockedAmount])",
                oldStored: true);
        }
    }
}
