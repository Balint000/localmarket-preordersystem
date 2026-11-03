using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Orders.AcceptOrder
{
    public sealed record AcceptOrderCommand(int OrderId);

    /// <summary>Az árus teljes egészében elfogadja a saját rendelését.</summary>
    public sealed class AcceptOrderHandler(IOrderRepository orders, IAuditLogRepository audit, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<OrderDto> HandleAsync(AcceptOrderCommand command, CancellationToken ct = default)
        {
            var order = await OrderSupport.LoadAsync(orders, command.OrderId, ct);
            OrderSupport.RequireProducerOwns(order, currentUser);

            var before = order.Status;
            order.Accept();
            await OrderSupport.AuditStatusAsync(audit, currentUser, "OrderAccepted", order, before, ct);
            await uow.SaveChangesAsync(ct);
            return OrderDto.From(order);
        }
    }
}
