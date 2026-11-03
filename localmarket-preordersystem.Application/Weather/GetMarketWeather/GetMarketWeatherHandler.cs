using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Weather.GetMarketWeather
{
    public sealed record GetMarketWeatherQuery(int MarketId, DateTime Date);

    public sealed record MarketWeatherDto(
        int MarketId, string MarketName, DateTime Date, bool IsMarketOpen, bool ForecastAvailable,
        double? TemperatureMinC, double? TemperatureMaxC, double? PrecipitationMm, string? Description);

    /// <summary>Időjárás a piac napjára. Ha a külső szolgáltatás nem elérhető, a válasz ForecastAvailable=false (a rendelés nem függ tőle).</summary>
    public sealed class GetMarketWeatherHandler(IMarketRepository markets, IWeatherService weather)
    {
        public async Task<MarketWeatherDto> HandleAsync(GetMarketWeatherQuery query, CancellationToken ct = default)
        {
            var market = await markets.GetByIdAsync(query.MarketId, ct)
                ?? throw new NotFoundException($"Nem található piac: {query.MarketId}.");
            if (market.Latitude is not double lat || market.Longitude is not double lon)
                throw new ConflictException("A piachoz nincs koordináta megadva, az időjárás nem kérdezhető le.");

            var open = market.OpeningDays.Contains(query.Date.DayOfWeek);
            DailyForecast? forecast = null;
            try
            {
                forecast = await weather.GetDailyForecastAsync(lat, lon, query.Date.Date, ct);
            }
            catch (HttpRequestException)
            {
                // a külső szolgáltatás hibája nem akadályozhatja a használatot
            }

            return forecast is null
                ? new MarketWeatherDto(market.Id, market.Name, query.Date.Date, open, false, null, null, null, null)
                : new MarketWeatherDto(market.Id, market.Name, query.Date.Date, open, true,
                    forecast.TemperatureMinC, forecast.TemperatureMaxC, forecast.PrecipitationMm, WeatherDescriptions.FromWmoCode(forecast.WeatherCode));
        }
    }

    public static class WeatherDescriptions
    {
        /// <summary>WMO időjárás-kód (Open-Meteo) magyar leírása.</summary>
        public static string FromWmoCode(int code) => code switch
        {
            0 => "Derült",
            1 or 2 or 3 => "Változóan felhős",
            45 or 48 => "Ködös",
            >= 51 and <= 57 => "Szitálás",
            >= 61 and <= 67 => "Eső",
            >= 71 and <= 77 => "Havazás",
            >= 80 and <= 82 => "Zápor",
            85 or 86 => "Hózápor",
            >= 95 and <= 99 => "Zivatar",
            _ => "Ismeretlen időjárás"
        };
    }
}
