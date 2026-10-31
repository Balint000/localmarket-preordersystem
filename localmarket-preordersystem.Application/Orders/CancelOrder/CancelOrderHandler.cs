using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Orders.CancelOrder
{
    public sealed record CancelOrderCommand(int OrderId);

    /// <summary>Lemondás: a vevő a sajátját, az árus a saját rendelését, a piacvezető bármelyiket. Készlet és idősáv-hely visszakerül.</summary>
    public sealed class CancelOrderHandler(IOrderRepository orders, IProductRepository products, IAuditLogRepository audit, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<OrderDto> HandleAsync(CancelOrderCommand command, CancellationToken ct = default)
        {
            var order = await OrderSupport.LoadAsync(orders, command.OrderId, ct);
            OrderSupport.RequireCanView(order, currentUser);

            var before = order.Status;
            var held = order.Cancel();
            await OrderSupport.ReleaseAsync(products, order, held, releaseSlot: held.Count > 0 || before is OrderStatus.Pending or OrderStatus.Accepted or OrderStatus.PartiallyFulfilled or OrderStatus.Ready, ct);
            await OrderSupport.AuditStatusAsync(audit, currentUser, "OrderCancelled", order, before, ct);
            await uow.SaveChangesAsync(ct);
            return OrderDto.From(order);
        }
    }
}
