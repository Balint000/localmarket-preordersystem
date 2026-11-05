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
    public async Task<IActionResult> Get(int marketId, [FromQuery] DateTime date, GetMarketWeatherHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new GetMarketWeatherQuery(marketId, date), ct));
}
