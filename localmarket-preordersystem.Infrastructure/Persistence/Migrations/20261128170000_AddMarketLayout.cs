using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace localmarket_preordersystem.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// A piac felülnézeti elrendezése (standok a rácson) jsonb oszlopban. A meglévő piacok üres 12 × 8-as rácsot
    /// kapnak. A korábban szabadon megadott, ütköző standszámokat (ugyanazon a piacon több árusnál) rendbe tesszük:
    /// a legkorábban regisztrált árus megtartja, a többinél "nincs kiosztva" (0) lesz — a piacvezető osztja ki újra.
    /// </summary>
    public partial class AddMarketLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Layout",
                table: "Markets",
                type: "jsonb",
                nullable: false,
                defaultValue: "{\"columns\":12,\"rows\":8,\"stalls\":[]}");

            migrationBuilder.Sql(@"
UPDATE ""Producers"" p SET ""StallNumber"" = 0
WHERE p.""StallNumber"" > 0 AND EXISTS (
    SELECT 1 FROM ""Producers"" o
    WHERE o.""MarketId"" = p.""MarketId"" AND o.""StallNumber"" = p.""StallNumber"" AND o.""Id"" < p.""Id"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Layout", table: "Markets");
        }
    }
}
