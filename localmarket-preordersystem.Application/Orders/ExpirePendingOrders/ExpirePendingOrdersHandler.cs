using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;

namespace localmarket_preordersystem.Application.Orders.ExpirePendingOrders
{
    /// <summary>
    /// Rendszerfeladat (háttérszolgáltatás hívja, nincs felhasználó): azokat a függő rendeléseket, amelyekre az
    /// árus az átvételi idősáv végéig nem válaszolt, „lejárt” állapotba teszi, és felszabadítja a foglalásukat.
    /// Egy hívás legfeljebb <c>batchSize</c> rendelést zár le; a többit a következő futás veszi fel.
    /// </summary>
    public sealed class ExpirePendingOrdersHandler(
        IOrderRepository orders,
        IProductRepository products,
        IAuditLogRepository audit,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        TimeProvider time)
    {
        public async Task<int> HandleAsync(int batchSize = 200, CancellationToken ct = default)
        {
            var now = time.GetLocalNow().DateTime;
            var stale = await orders.ListPendingWithEndedSlotAsync(now, batchSize, ct);
            if (stale.Count == 0)
                return 0;

            foreach (var order in stale)
            {
                var before = order.Status;
                var held = order.Expire(now);
                await OrderSupport.ReleaseAsync(products, order, held, releaseSlot: true, ct);
                await OrderSupport.AuditStatusAsync(audit, currentUser, "OrderExpired", order, before, ct);
            }

            await uow.SaveChangesAsync(ct);
            return stale.Count;
        }
    }
}
