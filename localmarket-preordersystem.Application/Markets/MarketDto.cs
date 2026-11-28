using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Markets
{
    /// <param name="OpensAt">Nyitás a piacnapokon (helyi idő); az átvételi idősávok ezen belül lehetnek.</param>
    /// <param name="ClosesAt">Zárás a piacnapokon (helyi idő).</param>
    public sealed record MarketDto(int Id, string Name, string Address, bool IsActive, double? Latitude, double? Longitude, IReadOnlyList<string> OpeningDays, TimeOnly OpensAt, TimeOnly ClosesAt)
    {
        public static MarketDto From(Market m) => new(
            m.Id, m.Name, m.Address, m.IsActive, m.Latitude, m.Longitude,
            m.OpeningDays.OrderBy(d => ((int)d + 6) % 7).Select(d => d.ToString()).ToList(),
            m.OpensAt, m.ClosesAt);
    }
}
