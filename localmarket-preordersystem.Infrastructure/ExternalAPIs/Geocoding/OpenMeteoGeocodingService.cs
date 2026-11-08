using System.Text.Json;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace localmarket_preordersystem.Infrastructure.ExternalAPIs.Geocoding;

/// <summary>
/// Open-Meteo geocoding (kulcs nélkül). Az API településnév alapú keresésre való, ezért a cím
/// vesszővel tagolt részeit hátulról előre próbáljuk (pl. "Fő tér 1, Zalaegerszeg" → "Zalaegerszeg", majd "Fő tér 1").
/// Az időjárás kiegészítő adat: hibánál null a válasz, kivétel nem szivárog ki.
/// </summary>
internal sealed class OpenMeteoGeocodingService(HttpClient http, ILogger<OpenMeteoGeocodingService> logger) : IGeocodingService
{
    public async Task<GeoPoint?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        try
        {
            foreach (var candidate in Candidates(address))
            {
                var point = await SearchAsync(candidate, cancellationToken);
                if (point is not null)
                    return point;
            }
            return null;
        }
        catch (Exception ex) when (
            ex is HttpRequestException or JsonException or InvalidOperationException or FormatException
            || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning(ex, "A geokódoló szolgáltatás nem elérhető vagy hibás választ adott.");
            return null;
        }
    }

    private static IEnumerable<string> Candidates(string address)
    {
        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (var i = parts.Length - 1; i >= 0; i--)
            yield return parts[i];

        // irányítószám/házszám nélküli változat: az első olyan szó, ami betűvel kezdődik
        var words = address.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
            .Where(w => char.IsLetter(w[0]) && w.Length > 2);
        foreach (var word in words)
            yield return word;
    }

    private async Task<GeoPoint?> SearchAsync(string name, CancellationToken ct)
    {
        var url = $"v1/search?name={Uri.EscapeDataString(name)}&count=1&language=hu&format=json";
        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            return null;

        var first = results[0];
        return new GeoPoint(first.GetProperty("latitude").GetDouble(), first.GetProperty("longitude").GetDouble());
    }
}
