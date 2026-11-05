using System.Globalization;
using System.Text.Json;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;

namespace localmarket_preordersystem.Infrastructure.ExternalAPIs.Weather;

internal sealed class OpenMeteoWeatherService(HttpClient http) : IWeatherService
{
    public async Task<DailyForecast?> GetDailyForecastAsync(
        double latitude, double longitude, DateTime date, CancellationToken cancellationToken = default)
    {
        var day = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lon = longitude.ToString(CultureInfo.InvariantCulture);

        var url = $"v1/forecast?latitude={lat}&longitude={lon}" +
                  "&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,weather_code" +
                  $"&start_date={day}&end_date={day}&timezone=auto";

        using var response = await http.GetAsync(url, cancellationToken);

        // Túl távoli / érvénytelen dátum: nincs előrejelzés
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            return null;

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!doc.RootElement.TryGetProperty("daily", out var daily))
            return null;

        var maxArr = daily.GetProperty("temperature_2m_max");
        if (maxArr.GetArrayLength() == 0 || maxArr[0].ValueKind == JsonValueKind.Null)
            return null;

        return new DailyForecast(
            date.Date,
            daily.GetProperty("temperature_2m_min")[0].GetDouble(),
            maxArr[0].GetDouble(),
            daily.GetProperty("precipitation_sum")[0].GetDouble(),
            daily.GetProperty("weather_code")[0].GetInt32());
    }
}
