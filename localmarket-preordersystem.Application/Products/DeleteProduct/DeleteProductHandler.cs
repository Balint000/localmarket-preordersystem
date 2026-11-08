using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;

namespace localmarket_preordersystem.Application.Products.DeleteProduct
{
    public sealed record DeleteProductCommand(int ProductId);

    /// <summary>
    /// Termék törlése — az árus csak a SAJÁT termékét törölheti. Ha a termékre már érkezett rendelés
    /// (akár lemondott), a rendelési előzmények épsége miatt nem törölhető; ilyenkor a heti
    /// mennyiség 0-ra állításával kivehető a kínálatból. A vevők kosaraiból a termék kikerül.
    /// </summary>
    public sealed class DeleteProductHandler(
        IProductRepository products,
        ICartRepository carts,
        IAuditLogRepository audit,
        IImageStorage images,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        public async Task HandleAsync(DeleteProductCommand command, CancellationToken ct = default)
        {
            var producerId = currentUser.RequireProducerId();

            var product = await products.GetByIdAsync(command.ProductId, ct)
                ?? throw new NotFoundException($"Nem található termék: {command.ProductId}.");
            if (product.ProducerId != producerId)
                throw new ForbiddenAccessException("Csak a saját termékedet törölheted.");

            if (await products.HasOrdersAsync(product.Id, ct))
                throw new ConflictException("A termékre már érkezett rendelés, ezért nem törölhető. Ha nem kínálod tovább, állítsd a heti mennyiséget 0-ra.");

            var imageFile = product.ImageFileName;

            await carts.RemoveItemsForProductAsync(product.Id, ct);
            products.Remove(product);
            await audit.RecordAsync(currentUser, "ProductDeleted", "Product", product.Id, new { product.Name, product.ProducerId }, null, ct);
            await uow.SaveChangesAsync(ct);

            // a fájl törlése a sikeres mentés után: hibánál nem marad kép nélküli termék
            if (imageFile is not null)
                await images.DeleteAsync(imageFile, ct);
        }
    }
}
