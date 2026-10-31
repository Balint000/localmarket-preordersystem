using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Orders.MarkOrderNotPickedUp
{
    public sealed record MarkOrderNotPickedUpCommand(int OrderId);

    /// <summary>Az árus jelzi, hogy a rendelést nem vették át.</summary>
    public sealed class MarkOrderNotPickedUpHandler(IOrderRepository orders, IAuditLogRepository audit, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<OrderDto> HandleAsync(MarkOrderNotPickedUpCommand command, CancellationToken ct = default)
        {
            var order = await OrderSupport.LoadAsync(orders, command.OrderId, ct);
            OrderSupport.RequireProducerOwns(order, currentUser);

            var before = order.Status;
            order.MarkNotPickedUp();
            await OrderSupport.AuditStatusAsync(audit, currentUser, "OrderNotPickedUp", order, before, ct);
            await uow.SaveChangesAsync(ct);
            return OrderDto.From(order);
        }
    }
}
