using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Application.Orders;

namespace localmarket_preordersystem.Application.Products.DeleteProduct
{
    /// <param name="Reason">Piacvezetőnél kötelező, ha a terméknek aktív rendelése van: a lemondott rendeléseknél a vevő ezt látja.</param>
    public sealed record DeleteProductCommand(int ProductId, string? Reason = null);

    /// <param name="Archived">true: a terméknek volt rendelése, ezért archiválva lett (a kínálatból eltűnik, az előzmény megmarad); false: fizikailag törölve.</param>
    /// <param name="CancelledOrderIds">A törlés miatt lemondott aktív rendelések.</param>
    /// <param name="UntouchedOrderIds">Aktív állapotú, de már lejárt idősávú rendelések: ezeket nem mondtuk le (átvétel / nem jött el / vita zárja).</param>
    public sealed record DeleteProductResultDto(int ProductId, bool Archived, IReadOnlyList<int> CancelledOrderIds, IReadOnlyList<int> UntouchedOrderIds);

    /// <summary>
    /// Termék törlése.
    /// <list type="bullet">
    /// <item>Árus: csak a saját termékét, és csak ha nincs rá aktív (függő/elfogadott/átvehető) rendelés.</item>
    /// <item>Piacvezető: bármelyik terméket; az aktív rendeléseket (amelyek idősávja még nem járt le) indoklással
    /// lemondja, a készletet és az idősáv-helyet felszabadítja.</item>
    /// </list>
    /// Rendelési előzmény esetén a termék archiválódik (soft delete), különben fizikailag törlődik a képével együtt.
    /// A vevők kosaraiból mindkét esetben kikerül.
    /// </summary>
    public sealed class DeleteProductHandler(
        IProductRepository products,
        IOrderRepository orders,
        ICartRepository carts,
        IAuditLogRepository audit,
        IImageStorage images,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        TimeProvider time)
    {
        public async Task<DeleteProductResultDto> HandleAsync(DeleteProductCommand command, CancellationToken ct = default)
        {
            var isManager = currentUser.IsManager();
            int? producerId = isManager ? null : currentUser.RequireProducerId();
            if (isManager) currentUser.RequireUserId();
            new ValidationBuilder().MaxLength(nameof(command.Reason), command.Reason, 500).ThrowIfInvalid();

            var product = await products.GetByIdAsync(command.ProductId, ct)
                ?? throw new NotFoundException($"Nem található termék: {command.ProductId}.");
            if (producerId is not null && product.ProducerId != producerId)
                throw new ForbiddenAccessException("Csak a saját termékedet törölheted.");
            if (product.IsArchived)
                throw new ConflictException("A termék már törölve (archiválva) van.");

            var now = time.GetLocalNow().DateTime;
            var active = await orders.ListActiveContainingProductAsync(product.Id, ct);
            var cancelled = new List<int>();
            var untouched = new List<int>();

            if (active.Count > 0)
            {
                if (!isManager)
                    throw new ConflictException(
                        $"A terméknek {active.Count} aktív rendelése van (#{string.Join(", #", active.Select(o => o.Id))}). " +
                        "Előbb teljesítsd vagy mondd le őket; ha nem kínálod tovább, állítsd a heti mennyiséget 0-ra.");

                new ValidationBuilder()
                    .Required(nameof(command.Reason), command.Reason,
                        $"A terméknek {active.Count} aktív rendelése van, ezeket a törlés lemondja: add meg az indoklást (a vásárlók látni fogják).")
                    .ThrowIfInvalid();

                foreach (var order in active)
                {
                    if (order.PickupSlot.HasEnded(now))
                    {
                        untouched.Add(order.Id);
                        continue;
                    }
                    var before = order.Status;
                    var held = order.CancelBySeller(now, $"A termék ({product.Name}) törölve a kínálatból: {command.Reason!.Trim()}");
                    await OrderSupport.ReleaseAsync(products, order, held, releaseSlot: true, ct);
                    await OrderSupport.AuditStatusAsync(audit, currentUser, "OrderCancelled", order, before, ct);
                    cancelled.Add(order.Id);
                }
            }

            await carts.RemoveItemsForProductAsync(product.Id, ct);

            var archive = await products.HasOrdersAsync(product.Id, ct);
            var imageFile = product.ImageFileName;
            if (archive)
                product.Archive();
            else
                products.Remove(product);

            await audit.RecordAsync(currentUser, archive ? "ProductArchived" : "ProductDeleted", "Product", product.Id,
                new { product.Name, product.ProducerId },
                new { Archived = archive, CancelledOrders = cancelled, Reason = command.Reason?.Trim() }, ct);
            await uow.SaveChangesAsync(ct);

            if (!archive && imageFile is not null)
                await images.DeleteAsync(imageFile, ct);

            return new DeleteProductResultDto(product.Id, archive, cancelled, untouched);
        }
    }
}
