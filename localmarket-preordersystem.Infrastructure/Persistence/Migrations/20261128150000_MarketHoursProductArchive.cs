using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace localmarket_preordersystem.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// A piac nyitvatartási órái (meglévő piacoknál 07:00–13:00), a termék archiválása (soft delete),
    /// és a termék nevének egyedisége csak a nem archivált termékekre.
    /// </summary>
    public partial class MarketHoursProductArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "OpensAt",
                table: "Markets",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(7, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ClosesAt",
                table: "Markets",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(13, 0, 0));

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.DropIndex(
                name: "IX_Products_ProducerId_Name",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProducerId_Name",
                table: "Products",
                columns: new[] { "ProducerId", "Name" },
                unique: true,
                filter: "\"IsArchived\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_ProducerId_Name",
                table: "Products");

            migrationBuilder.DropColumn(name: "IsArchived", table: "Products");
            migrationBuilder.DropColumn(name: "ClosesAt", table: "Markets");
            migrationBuilder.DropColumn(name: "OpensAt", table: "Markets");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProducerId_Name",
                table: "Products",
                columns: new[] { "ProducerId", "Name" },
                unique: true);
        }
    }
}
