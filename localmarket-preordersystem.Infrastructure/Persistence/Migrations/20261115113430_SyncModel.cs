using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace localmarket_preordersystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Amount",
                table: "Payments");

            migrationBuilder.AddColumn<string>(
                name: "ImageFileName",
                table: "Products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Amount",
                table: "Payments",
                sql: "\"Amount\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Amount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ImageFileName",
                table: "Products");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Amount",
                table: "Payments",
                sql: "\"Amount\" > 0");
        }
    }
}
