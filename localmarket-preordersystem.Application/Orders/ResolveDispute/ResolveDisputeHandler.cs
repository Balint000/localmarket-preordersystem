using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Orders.ResolveDispute
{
    public enum DisputeDecision { PickedUp = 1, Cancelled = 2 }

    public sealed record ResolveDisputeCommand(int OrderId, DisputeDecision Decision, string? Note);

    /// <summary>A piacvezető lezárja a vitatott rendelést (átvettnek tekinti vagy lemondja; utóbbinál a teljesített fizetés visszatérül).</summary>
    public sealed class ResolveDisputeHandler(IOrderRepository orders, IAuditLogRepository audit, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<OrderDto> HandleAsync(ResolveDisputeCommand command, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Admin);
            new ValidationBuilder()
                .Must(nameof(command.Decision), Enum.IsDefined(command.Decision), "Érvénytelen döntés.")
                .MaxLength(nameof(command.Note), command.Note, 1000)
                .ThrowIfInvalid();

            var order = await OrderSupport.LoadAsync(orders, command.OrderId, ct);
            var before = order.Status;
            if (command.Decision == DisputeDecision.PickedUp)
                order.ResolveDisputeAsPickedUp(command.Note ?? string.Empty);
            else
                order.ResolveDisputeAsCancelled(command.Note ?? string.Empty);

            await OrderSupport.AuditStatusAsync(audit, currentUser, "DisputeResolved", order, before, ct);
            await uow.SaveChangesAsync(ct);
            return OrderDto.From(order);
        }
    }
}
