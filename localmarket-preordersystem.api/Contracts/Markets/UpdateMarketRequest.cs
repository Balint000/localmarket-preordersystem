namespace localmarket_preordersystem.api.Contracts.Markets;

public sealed record UpdateMarketRequest(
    string Name, string Address, IReadOnlyList<DayOfWeek> OpeningDays,
    double? Latitude, double? Longitude, bool IsActive,
    TimeOnly? OpensAt = null, TimeOnly? ClosesAt = null);
