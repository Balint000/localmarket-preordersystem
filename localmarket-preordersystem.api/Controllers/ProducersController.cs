using localmarket_preordersystem.api.Contracts.Orders;
using localmarket_preordersystem.api.Extensions;
using localmarket_preordersystem.Application.Producers.ApproveProducer;
using localmarket_preordersystem.Application.Producers.GetMyProducer;
using localmarket_preordersystem.Application.Producers.ListProducers;
using localmarket_preordersystem.Application.Producers.RejectProducer;
using localmarket_preordersystem.Application.Producers.UpdateMyProducer;
using localmarket_preordersystem.Domain.ValueObject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Controllers;

[ApiController]
[Route("api/producers")]
public class ProducersController : ControllerBase
{
    // Anonymous callers only see approved producers (enforced in handler).
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List([FromQuery] ProducerStatus? status, [FromQuery] int? marketId, ListProducersHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new ListProducersQuery(status, marketId), ct));

    // Works for Pending/Rejected producers too (own profile incl. rejection reason).
    [HttpGet("me")]
    [Authorize(Policy = Policies.Producer)]
    public async Task<IActionResult> GetMine(GetMyProducerHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(ct));

    [HttpPut("me")]
    [Authorize(Policy = Policies.Producer)]
    public async Task<IActionResult> UpdateMine(UpdateMyProducerCommand command, UpdateMyProducerHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(command, ct));

    [HttpPost("{id:int}/approve")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Approve(int id, ApproveProducerHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new ApproveProducerCommand(id), ct));

    [HttpPost("{id:int}/reject")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Reject(int id, ReasonRequest body, RejectProducerHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new RejectProducerCommand(id, body.Reason), ct));
}
