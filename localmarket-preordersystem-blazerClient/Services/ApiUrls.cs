namespace localmarket_preordersystem.Services;

/// <summary>
/// Az API böngészőből elérhető címe (Api:PublicBaseUrl). A szerver-szerver hívás címe (Api:BaseUrl, pl. http://api:8080
/// Dockerben) a böngészőből nem feltétlenül érhető el, a feltöltött képeket viszont a böngésző tölti le.
/// </summary>
public sealed class ApiUrls(string publicBaseUrl)
{
    private readonly string _base = publicBaseUrl.TrimEnd('/');

    /// <summary>A termékkép abszolút címe; null, ha a terméknek nincs képe.</summary>
    public string? Image(string? relativePath)
        => string.IsNullOrWhiteSpace(relativePath) ? null : _base + "/" + relativePath.TrimStart('/');
}
