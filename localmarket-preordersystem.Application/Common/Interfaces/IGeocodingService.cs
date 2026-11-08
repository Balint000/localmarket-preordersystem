using localmarket_preordersystem.Application.Common.Models;

namespace localmarket_preordersystem.Application.Common.Interfaces;

/// <summary>Cím → koordináta (pl. Open-Meteo geocoding); Infrastructure implementálja, HttpClient-tel.</summary>
public interface IGeocodingService
{
    /// <summary>Null, ha a címhez nem található hely, vagy a szolgáltatás nem elérhető (nem dob hálózati hibát).</summary>
    Task<GeoPoint?> GeocodeAsync(string address, CancellationToken cancellationToken = default);
}
