using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace localmarket_preordersystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductImageAndStockPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductStocks_Quantity",
                table: "ProductStocks");

            migrationBuilder.AddColumn<string>(
                name: "ImageFileName",
                table: "Products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "ProductStocks",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ReservedQuantity",
                table: "ProductStocks",
                type: "numeric(12,3)",
                precision: 12,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "ProductStocks",
                type: "numeric(12,3)",
                precision: 12,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductStocks_Quantity",
                table: "ProductStocks",
                sql: "\"Quantity\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductStocks_Quantity",
                table: "ProductStocks");

            migrationBuilder.DropColumn(
                name: "ImageFileName",
                table: "Products");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "ProductStocks",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "ReservedQuantity",
                table: "ProductStocks",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,3)",
                oldPrecision: 12,
                oldScale: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "ProductStocks",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,3)",
                oldPrecision: 12,
                oldScale: 3);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductStocks_Quantity",
                table: "ProductStocks",
                sql: "\"Quantity\" > 0");
        }
    }
}
