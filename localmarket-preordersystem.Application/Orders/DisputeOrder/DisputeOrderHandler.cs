using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Orders.DisputeOrder
{
    public sealed record DisputeOrderCommand(int OrderId, string Note);

    /// <summary>Vitatás: a rendelés vevője vagy az érintett árus jelezheti; a piacvezető dönt róla.</summary>
    public sealed class DisputeOrderHandler(IOrderRepository orders, IAuditLogRepository audit, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<OrderDto> HandleAsync(DisputeOrderCommand command, CancellationToken ct = default)
        {
            var order = await OrderSupport.LoadAsync(orders, command.OrderId, ct);
            if (currentUser.IsManager())
                throw new ForbiddenAccessException("A piacvezető nem vitat, hanem dönt a vitás rendelésekről.");
            OrderSupport.RequireCanView(order, currentUser);
            new ValidationBuilder()
                .Required(nameof(command.Note), command.Note, "A vitatás indoklása kötelező.")
                .MaxLength(nameof(command.Note), command.Note, 1000)
                .ThrowIfInvalid();

            var before = order.Status;
            order.Dispute(command.Note);
            await OrderSupport.AuditStatusAsync(audit, currentUser, "OrderDisputed", order, before, ct);
            await uow.SaveChangesAsync(ct);
            return OrderDto.From(order);
        }
    }
}
