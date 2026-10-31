using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Orders.RejectOrder
{
    public sealed record RejectOrderCommand(int OrderId, string Reason);

    /// <summary>Az árus elutasítja a függő rendelést; készlet és idősáv-hely felszabadul.</summary>
    public sealed class RejectOrderHandler(IOrderRepository orders, IProductRepository products, IAuditLogRepository audit, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<OrderDto> HandleAsync(RejectOrderCommand command, CancellationToken ct = default)
        {
            var order = await OrderSupport.LoadAsync(orders, command.OrderId, ct);
            OrderSupport.RequireProducerOwns(order, currentUser);
            new ValidationBuilder()
                .Required(nameof(command.Reason), command.Reason, "Az elutasítás indoklása kötelező.")
                .MaxLength(nameof(command.Reason), command.Reason, 500)
                .ThrowIfInvalid();

            var before = order.Status;
            var held = order.Reject(command.Reason);
            await OrderSupport.ReleaseAsync(products, order, held, releaseSlot: true, ct);
            await OrderSupport.AuditStatusAsync(audit, currentUser, "OrderRejected", order, before, ct);
            await uow.SaveChangesAsync(ct);
            return OrderDto.From(order);
        }
    }
}
