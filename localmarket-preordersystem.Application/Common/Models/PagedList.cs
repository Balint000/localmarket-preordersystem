namespace localmarket_preordersystem.Application.Common.Models
{
    /// <summary>Adatbázis-szinten lapozott eredmény: az aktuális oldal elemei és a szűrésnek megfelelő teljes darabszám.</summary>
    public sealed record PagedList<T>(IReadOnlyList<T> Items, int Total);
}
