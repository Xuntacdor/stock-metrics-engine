using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFeeAndTaxToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Fee",
                table: "Transactions",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Tax",
                table: "Transactions",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Symbol",
                table: "Transactions",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Transactions",
                type: "decimal(18,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Fee", table: "Transactions");
            migrationBuilder.DropColumn(name: "Tax", table: "Transactions");
            migrationBuilder.DropColumn(name: "Symbol", table: "Transactions");
            migrationBuilder.DropColumn(name: "Quantity", table: "Transactions");
            migrationBuilder.DropColumn(name: "Price", table: "Transactions");
        }
    }
}
