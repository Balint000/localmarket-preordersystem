using localmarket_preordersystem.api.Extensions;
using localmarket_preordersystem.Application.PickupSlots.CreatePickupSlot;
using localmarket_preordersystem.Application.PickupSlots.ListPickupSlots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Controllers;

[ApiController]
[Route("api/pickup-slots")]
public class PickupSlotsController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List([FromQuery] int producerId, ListPickupSlotsHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(producerId, ct));

    [HttpPost]
    [Authorize(Policy = Policies.Producer)]
    public async Task<IActionResult> Create(CreatePickupSlotCommand command, CreatePickupSlotHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(command, ct));
}
