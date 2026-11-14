using localmarket_preordersystem.api.Extensions;
using localmarket_preordersystem.api.Contracts.PickupSlots;
using localmarket_preordersystem.Application.PickupSlots.CreatePickupSlot;
using localmarket_preordersystem.Application.PickupSlots.DeletePickupSlot;
using localmarket_preordersystem.Application.PickupSlots.ListPickupSlots;
using localmarket_preordersystem.Application.PickupSlots.UpdatePickupSlot;
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

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.Producer)]
    public async Task<IActionResult> Update(int id, PickupSlotRequest body, UpdatePickupSlotHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new UpdatePickupSlotCommand(id, body.Date, body.StartTime, body.EndTime, body.Capacity), ct));

    // 409 if the slot has bookings or any (even cancelled) order.
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.Producer)]
    public async Task<IActionResult> Delete(int id, DeletePickupSlotHandler handler, CancellationToken ct)
    {
        await handler.HandleAsync(new DeletePickupSlotCommand(id), ct);
        return NoContent();
    }
}
