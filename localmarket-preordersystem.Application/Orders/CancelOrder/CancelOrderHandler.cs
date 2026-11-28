using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Orders.CancelOrder
{
    /// <param name="Reason">Árusnál és piacvezetőnél kötelező (a vevő látja); vevőnél nem kötelező, a naplóba kerül.</param>
    public sealed record CancelOrderCommand(int OrderId, string? Reason = null);

    /// <summary>
    /// Lemondás. A vevő a sajátját a lemondási határidőig (<see cref="OrderPolicy"/>), az árus a saját és a
    /// piacvezető bármelyik aktív rendelést indoklással, az átvételi idősáv végéig. Készlet és idősáv-hely visszakerül.
    /// </summary>
    public sealed class CancelOrderHandler(
        IOrderRepository orders,
        IProductRepository products,
        IAuditLogRepository audit,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        TimeProvider time,
        OrderPolicy policy)
    {
        public async Task<OrderDto> HandleAsync(CancelOrderCommand command, CancellationToken ct = default)
        {
            new ValidationBuilder()
                .MaxLength(nameof(command.Reason), command.Reason, 500)
                .ThrowIfInvalid();

            var order = await OrderSupport.LoadAsync(orders, command.OrderId, ct);
            var now = time.GetLocalNow().DateTime;
            var before = order.Status;

            IReadOnlyList<ReservedItem> held;
            if (currentUser.IsManager())
            {
                RequireReason(command);
                held = order.CancelBySeller(now, command.Reason!);
            }
            else if (currentUser.Role == Role.Producer)
            {
                OrderSupport.RequireProducerOwns(order, currentUser);
                RequireReason(command);
                held = order.CancelBySeller(now, command.Reason!);
            }
            else
            {
                currentUser.RequireRole(Role.Customer);
                if (order.UserId != currentUser.RequireUserId())
                    throw new ForbiddenAccessException("Csak a saját rendelésedet mondhatod le.");
                held = order.CancelByCustomer(now, policy.CustomerCancellationCutoff);
            }

            // a lemondás csak aktív (helyet foglaló) állapotból lehetséges, így az idősáv-hely is felszabadul
            await OrderSupport.ReleaseAsync(products, order, held, releaseSlot: true, ct);
            await audit.RecordAsync(currentUser, "OrderCancelled", "Order", order.Id,
                new { Status = before },
                new { order.Status, CancelledBy = currentUser.Role?.ToString(), Reason = command.Reason?.Trim() }, ct);
            await uow.SaveChangesAsync(ct);
            return OrderDto.From(order);
        }

        private static void RequireReason(CancelOrderCommand command) => new ValidationBuilder()
            .Required(nameof(command.Reason), command.Reason, "A lemondás indoklása kötelező (a vásárló látni fogja).")
            .ThrowIfInvalid();
    }
}
