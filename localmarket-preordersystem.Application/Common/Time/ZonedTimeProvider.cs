namespace localmarket_preordersystem.Application.Common.Time
{
    /// <summary>
    /// A "helyi idő" egy megadott időzónát jelent (alapértelmezés: Europe/Budapest), nem a szerver beállítását.
    /// Kell hozzá: a piacok átvételi időpontjait (dátum + óra) helyi időben adják meg, a konténerben futó szerver
    /// viszont jellemzően UTC-ben van, így a "lejárt-e az idősáv" ellenőrzés órákat csúszna.
    /// </summary>
    public sealed class ZonedTimeProvider(TimeZoneInfo zone) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone { get; } = zone;

        /// <summary>Időzóna-azonosító (IANA vagy Windows) alapján; ismeretlen/üres azonosítónál a rendszer helyi zónája.</summary>
        public static ZonedTimeProvider Create(string? timeZoneId)
        {
            if (!string.IsNullOrWhiteSpace(timeZoneId))
            {
                try
                {
                    return new ZonedTimeProvider(TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
                }
                catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
                {
                    // tzdata hiányzik / elírt azonosító: a rendszer helyi zónájára esünk vissza
                }
            }
            return new ZonedTimeProvider(TimeZoneInfo.Local);
        }
    }
}
