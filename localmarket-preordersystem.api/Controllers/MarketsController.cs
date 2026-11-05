using localmarket_preordersystem.Application.Markets.CreateMarket;
using localmarket_preordersystem.Application.Markets.ListMarkets;
using localmarket_preordersystem.Application.Markets.UpdateMarket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Controllers;

public sealed record UpdateMarketRequest(
    string Name, string Address, IReadOnlyList<DayOfWeek> OpeningDays,
    double? Latitude, double? Longitude, bool IsActive);

[ApiController]
[Route("api/markets")]
public class MarketsController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(ListMarketsHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(ct));

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(CreateMarketCommand command, CreateMarketHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(command, ct));

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, UpdateMarketRequest body, UpdateMarketHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(
            new UpdateMarketCommand(id, body.Name, body.Address, body.OpeningDays, body.Latitude, body.Longitude, body.IsActive), ct));
}
