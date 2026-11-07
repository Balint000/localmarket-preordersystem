using localmarket_preordersystem.api.Contracts.Orders;
using localmarket_preordersystem.api.Extensions;
using localmarket_preordersystem.api.Contracts.Common;
using localmarket_preordersystem.Application.Orders.AcceptOrder;
using localmarket_preordersystem.Application.Orders.AcceptOrderPartially;
using localmarket_preordersystem.Application.Orders.CancelOrder;
using localmarket_preordersystem.Application.Orders.DisputeOrder;
using localmarket_preordersystem.Application.Orders.GetOrder;
using localmarket_preordersystem.Application.Orders.ListDisputedOrders;
using localmarket_preordersystem.Application.Orders.ListMyOrders;
using localmarket_preordersystem.Application.Orders.ListProducerOrders;
using localmarket_preordersystem.Application.Orders.MarkOrderNotPickedUp;
using localmarket_preordersystem.Application.Orders.MarkOrderPickedUp;
using localmarket_preordersystem.Application.Orders.MarkOrderReady;
using localmarket_preordersystem.Application.Orders.PlaceOrder;
using localmarket_preordersystem.Application.Orders.RejectOrder;
using localmarket_preordersystem.Application.Orders.ResolveDispute;
using localmarket_preordersystem.Domain.ValueObject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    // FE: send one pickup slot per producer in the cart; show 409 as "stock changed, reload cart".
    [HttpPost]
    [Authorize(Policy = Policies.Customer)]
    public async Task<IActionResult> Place(PlaceOrderCommand command, PlaceOrderHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(command, ct));

    [HttpGet("my")]
    [Authorize(Policy = Policies.Customer)]
    public async Task<IActionResult> ListMy([FromQuery] OrderStatus? status, [FromQuery] PageRequest paging, ListMyOrdersHandler handler, CancellationToken ct)
        => Ok((await handler.HandleAsync(new ListMyOrdersQuery(status), ct)).ToPaged(paging));

    [HttpGet("producer")]
    [Authorize(Policy = Policies.Producer)]
    public async Task<IActionResult> ListProducer([FromQuery] OrderStatus? status, [FromQuery] PageRequest paging, ListProducerOrdersHandler handler, CancellationToken ct)
        => Ok((await handler.HandleAsync(new ListProducerOrdersQuery(status), ct)).ToPaged(paging));

    [HttpGet("disputed")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> ListDisputed([FromQuery] PageRequest paging, ListDisputedOrdersHandler handler, CancellationToken ct)
        => Ok((await handler.HandleAsync(ct)).ToPaged(paging));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, GetOrderHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(id, ct));

    [HttpPost("{id:int}/cancel")]
    [Authorize(Policy = Policies.Customer)]
    public async Task<IActionResult> Cancel(int id, CancelOrderHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new CancelOrderCommand(id), ct));

    [HttpPost("{id:int}/accept")]
    [Authorize(Policy = Policies.Producer)]
    public async Task<IActionResult> Accept(int id, AcceptOrderHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new AcceptOrderCommand(id), ct));

    [HttpPost("{id:int}/accept-partially")]
    [Authorize(Policy = Policies.Producer)]
    public async Task<IActionResult> AcceptPartially(int id, PartialRequest body, AcceptOrderPartiallyHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new AcceptOrderPartiallyCommand(id, body.Lines), ct));

    [HttpPost("{id:int}/reject")]
    [Authorize(Policy = Policies.Producer)]
    public async Task<IActionResult> Reject(int id, ReasonRequest body, RejectOrderHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new RejectOrderCommand(id, body.Reason), ct));

    [HttpPost("{id:int}/ready")]
    [Authorize(Policy = Policies.Producer)]
    public async Task<IActionResult> Ready(int id, MarkOrderReadyHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new MarkOrderReadyCommand(id), ct));

    [HttpPost("{id:int}/picked-up")]
    [Authorize(Policy = Policies.Producer)]
    public async Task<IActionResult> PickedUp(int id, MarkOrderPickedUpHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new MarkOrderPickedUpCommand(id), ct));

    [HttpPost("{id:int}/not-picked-up")]
    [Authorize(Policy = Policies.Producer)]
    public async Task<IActionResult> NotPickedUp(int id, MarkOrderNotPickedUpHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new MarkOrderNotPickedUpCommand(id), ct));

    [HttpPost("{id:int}/dispute")]
    [Authorize(Policy = Policies.Customer)]
    public async Task<IActionResult> Dispute(int id, NoteRequest body, DisputeOrderHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new DisputeOrderCommand(id, body.Note), ct));

    [HttpPost("{id:int}/resolve-dispute")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> ResolveDispute(int id, ResolveRequest body, ResolveDisputeHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new ResolveDisputeCommand(id, body.Decision, body.Note), ct));
}
