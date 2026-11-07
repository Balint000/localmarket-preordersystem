using localmarket_preordersystem.api.Contracts.Markets;
using localmarket_preordersystem.api.Extensions;
using localmarket_preordersystem.Application.Markets.CreateMarket;
using localmarket_preordersystem.Application.Markets.ListMarkets;
using localmarket_preordersystem.Application.Markets.UpdateMarket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Controllers;

[ApiController]
[Route("api/markets")]
public class MarketsController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(ListMarketsHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(ct));

    [HttpPost]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Create(CreateMarketCommand command, CreateMarketHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(command, ct));

    // FE: OpeningDays is an array of weekday names (e.g. "Saturday").
    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Update(int id, UpdateMarketRequest body, UpdateMarketHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(
            new UpdateMarketCommand(id, body.Name, body.Address, body.OpeningDays, body.Latitude, body.Longitude, body.IsActive), ct));
}
