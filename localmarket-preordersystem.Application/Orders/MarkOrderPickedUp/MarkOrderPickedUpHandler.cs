using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Orders.MarkOrderPickedUp
{
    public sealed record MarkOrderPickedUpCommand(int OrderId);

    /// <summary>Az árus rögzíti az átvételt (a függő fizetés teljesítettnek számít).</summary>
    public sealed class MarkOrderPickedUpHandler(IOrderRepository orders, IAuditLogRepository audit, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<OrderDto> HandleAsync(MarkOrderPickedUpCommand command, CancellationToken ct = default)
        {
            var order = await OrderSupport.LoadAsync(orders, command.OrderId, ct);
            OrderSupport.RequireProducerOwns(order, currentUser);

            var before = order.Status;
            order.MarkPickedUp();
            await OrderSupport.AuditStatusAsync(audit, currentUser, "OrderPickedUp", order, before, ct);
            await uow.SaveChangesAsync(ct);
            return OrderDto.From(order);
        }
    }
}
