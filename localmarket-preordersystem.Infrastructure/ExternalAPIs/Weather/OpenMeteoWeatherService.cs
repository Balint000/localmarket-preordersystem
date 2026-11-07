using System.Globalization;
using System.Text.Json;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace localmarket_preordersystem.Infrastructure.ExternalAPIs.Weather;

internal sealed class OpenMeteoWeatherService(HttpClient http, ILogger<OpenMeteoWeatherService> logger) : IWeatherService
{
    public async Task<DailyForecast?> GetDailyForecastAsync(
        double latitude, double longitude, DateTime date, CancellationToken cancellationToken = default)
    {
        try
        {
            return await FetchAsync(latitude, longitude, date, cancellationToken);
        }
        catch (Exception ex) when (
            ex is HttpRequestException or JsonException or KeyNotFoundException or InvalidOperationException or FormatException
            || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            // Weather is auxiliary data: degrade gracefully instead of failing the request.
            logger.LogWarning(ex, "Az időjárás-szolgáltatás nem elérhető vagy hibás választ adott.");
            return null;
        }
    }

    private async Task<DailyForecast?> FetchAsync(double latitude, double longitude, DateTime date, CancellationToken ct)
    {
        var day = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lon = longitude.ToString(CultureInfo.InvariantCulture);

        var url = $"v1/forecast?latitude={lat}&longitude={lon}" +
                  "&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,weather_code" +
                  $"&start_date={day}&end_date={day}&timezone=auto";

        using var response = await http.GetAsync(url, ct);

        // Too far ahead / invalid date: no forecast available.
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            return null;

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (!doc.RootElement.TryGetProperty("daily", out var daily))
            return null;

        var max = daily.GetProperty("temperature_2m_max");
        var min = daily.GetProperty("temperature_2m_min");
        var rain = daily.GetProperty("precipitation_sum");
        var code = daily.GetProperty("weather_code");

        if (max.GetArrayLength() == 0 || min.GetArrayLength() == 0 || rain.GetArrayLength() == 0 || code.GetArrayLength() == 0)
            return null;
        if (max[0].ValueKind == JsonValueKind.Null || min[0].ValueKind == JsonValueKind.Null || code[0].ValueKind == JsonValueKind.Null)
            return null;

        var precipitation = rain[0].ValueKind == JsonValueKind.Null ? 0d : rain[0].GetDouble();

        return new DailyForecast(date.Date, min[0].GetDouble(), max[0].GetDouble(), precipitation, code[0].GetInt32());
    }
}
