using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Markets.Layout
{
    /// <summary>Egy stand a rácson, a kiosztott árussal (ha van).</summary>
    public sealed record StallDto(int Number, int X, int Y, int Width, int Height, string? Label,
        int? ProducerId, string? ProducerName, string? ProducerStatus);

    /// <param name="UnassignedProducers">Csak mentéskor: azok az árusok, akiknek a standja megszűnt (vagy ütközött), ezért kiosztás nélkül maradtak.</param>
    public sealed record MarketLayoutDto(int MarketId, string MarketName, int Columns, int Rows, IReadOnlyList<StallDto> Stalls,
        IReadOnlyList<UnassignedProducerDto> UnassignedProducers)
    {
        public static MarketLayoutDto From(Market market, IEnumerable<Producer> producers, IReadOnlyList<UnassignedProducerDto>? unassigned = null)
        {
            // standonként egy árus: ütközés esetén (régi adat) a korábban regisztrált
            var byStall = producers
                .Where(p => p.StallNumber > 0)
                .GroupBy(p => p.StallNumber)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Id).First());
            var stalls = market.Layout.Stalls
                .Select(s => byStall.TryGetValue(s.Number, out var p)
                    ? new StallDto(s.Number, s.X, s.Y, s.Width, s.Height, s.Label, p.Id, p.Name, p.Status.ToString())
                    : new StallDto(s.Number, s.X, s.Y, s.Width, s.Height, s.Label, null, null, null))
                .ToList();
            return new MarketLayoutDto(market.Id, market.Name, market.Layout.Columns, market.Layout.Rows, stalls,
                unassigned ?? Array.Empty<UnassignedProducerDto>());
        }
    }

    public sealed record UnassignedProducerDto(int ProducerId, string ProducerName, int PreviousStallNumber, string Reason);

    /// <summary>Egy stand a mentendő elrendezésben.</summary>
    public sealed record StallInput(int Number, int X, int Y, int Width = 1, int Height = 1, string? Label = null)
    {
        public MarketStall ToDomain() => new(Number, X, Y, Width, Height, Label);
    }
}
