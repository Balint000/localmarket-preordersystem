using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Markets
{
    public sealed record MarketDto(int Id, string Name, string Address, bool IsActive, double? Latitude, double? Longitude, IReadOnlyList<string> OpeningDays)
    {
        public static MarketDto From(Market m) => new(
            m.Id, m.Name, m.Address, m.IsActive, m.Latitude, m.Longitude,
            m.OpeningDays.OrderBy(d => ((int)d + 6) % 7).Select(d => d.ToString()).ToList());
    }
}
