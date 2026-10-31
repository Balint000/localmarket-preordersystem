using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Orders.MarkOrderReady
{
    public sealed record MarkOrderReadyCommand(int OrderId);

    /// <summary>Az árus jelzi, hogy a rendelés átvehető.</summary>
    public sealed class MarkOrderReadyHandler(IOrderRepository orders, IAuditLogRepository audit, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<OrderDto> HandleAsync(MarkOrderReadyCommand command, CancellationToken ct = default)
        {
            var order = await OrderSupport.LoadAsync(orders, command.OrderId, ct);
            OrderSupport.RequireProducerOwns(order, currentUser);

            var before = order.Status;
            order.MarkReady();
            await OrderSupport.AuditStatusAsync(audit, currentUser, "OrderReady", order, before, ct);
            await uow.SaveChangesAsync(ct);
            return OrderDto.From(order);
        }
    }
}
