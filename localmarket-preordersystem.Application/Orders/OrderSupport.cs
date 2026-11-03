using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Orders
{
    /// <summary>A rendelés-use case-ek közös, apró segédei: betöltés, hozzáférés-ellenőrzés, készlet-felszabadítás, napló.</summary>
    internal static class OrderSupport
    {
        public static async Task<Order> LoadAsync(IOrderRepository orders, int orderId, CancellationToken ct)
            => await orders.GetByIdAsync(orderId, ct) ?? throw new NotFoundException($"Nem található rendelés: {orderId}.");

        /// <summary>Az árus csak a SAJÁT rendeléseit kezelheti (a ProducerId a tokenből jön).</summary>
        public static void RequireProducerOwns(Order order, ICurrentUserService user)
        {
            var producerId = user.RequireProducerId();
            if (order.ProducerId != producerId)
                throw new ForbiddenAccessException("Csak a saját rendeléseidet kezelheted.");
        }

        /// <summary>Megtekintés: a rendelés vevője, az érintett árus vagy a piacvezető.</summary>
        public static void RequireCanView(Order order, ICurrentUserService user)
        {
            var userId = user.RequireUserId();
            var allowed = user.IsManager()
                || order.UserId == userId
                || (user.Role == Role.Producer && user.ProducerId == order.ProducerId);
            if (!allowed)
                throw new ForbiddenAccessException("Ehhez a rendeléshez nincs hozzáférésed.");
        }

        /// <summary>Lefoglalt készlet visszaadása; teljes lemondásnál az idősáv-hely is felszabadul.</summary>
        public static async Task ReleaseAsync(IProductRepository products, Order order, IReadOnlyList<ReservedItem> released, bool releaseSlot, CancellationToken ct)
        {
            if (released.Count > 0)
            {
                var byId = (await products.GetByIdsAsync(released.Select(r => r.ProductId).Distinct(), ct)).ToDictionary(p => p.Id);
                foreach (var r in released)
                    if (byId.TryGetValue(r.ProductId, out var product))
                        product.ReleaseStockForPickup(order.PickupSlot.Date, r.Quantity);
            }
            if (releaseSlot)
                order.PickupSlot.Release();
        }

        public static Task AuditStatusAsync(IAuditLogRepository audit, ICurrentUserService user, string action, Order order, OrderStatus before, CancellationToken ct)
            => audit.RecordAsync(user, action, "Order", order.Id, new { Status = before }, new { Status = order.Status, order.DisputeNote, order.RejectionReason }, ct);
    }
}
