using localmarket_preordersystem.Application.Weather.GetMarketWeather;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/weather")]
public class WeatherController : ControllerBase
{
    [HttpGet("markets/{marketId:int}")]
    public async Task<IActionResult> Get(int marketId, [FromQuery] DateTime? date, GetMarketWeatherHandler handler, CancellationToken ct)
    {
        if (date is null) return Problem(statusCode: 400, title: "Hiányzó dátum", detail: "A 'date' paraméter kötelező (yyyy-MM-dd).");

        return Ok(await handler.HandleAsync(new GetMarketWeatherQuery(marketId, date.Value), ct));
    }
}
