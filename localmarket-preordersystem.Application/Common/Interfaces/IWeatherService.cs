using localmarket_preordersystem.Application.Common.Models;

namespace localmarket_preordersystem.Application.Common.Interfaces
{
    /// <summary>Külső időjárás API (pl. Open-Meteo) kapuja; Infrastructure implementálja, HttpClient-tel.</summary>
    public interface IWeatherService
    {
        /// <summary>Null, ha a dátumra nincs előrejelzés (pl. túl távoli nap). Kivételt csak hálózati hiba esetén dob.</summary>
        Task<DailyForecast?> GetDailyForecastAsync(double latitude, double longitude, DateTime date, CancellationToken cancellationToken = default);
    }
}
