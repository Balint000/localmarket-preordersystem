namespace localmarket_preordersystem.Services;

/// <summary>
/// A piac helyi ideje a kliens szerverén (a konténer UTC-ben fut). Csak megjelenítési döntésekhez
/// (melyik gomb látszik); az üzleti szabályt a backend kényszeríti ugyanebben az időzónában.
/// </summary>
public static class MarketClock
{
    private static readonly TimeZoneInfo Zone = Resolve(Environment.GetEnvironmentVariable("App__TimeZone") ?? "Europe/Budapest");

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    private static TimeZoneInfo Resolve(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException) { return TimeZoneInfo.Local; }
    }
}
