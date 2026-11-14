using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;

namespace localmarket_preordersystem.Application.Products.DeleteProductImage
{
    public sealed record DeleteProductImageCommand(int ProductId);

    /// <summary>A termék képének eltávolítása — csak a saját termékről.</summary>
    public sealed class DeleteProductImageHandler(
        IProductRepository products,
        IImageStorage images,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        TimeProvider time)
    {
        public async Task<ProductDto> HandleAsync(DeleteProductImageCommand command, CancellationToken ct = default)
        {
            var producerId = currentUser.RequireProducerId();

            var product = await products.GetByIdAsync(command.ProductId, ct)
                ?? throw new NotFoundException($"Nem található termék: {command.ProductId}.");
            if (product.ProducerId != producerId)
                throw new ForbiddenAccessException("Csak a saját terméked képét törölheted.");

            var previous = product.ImageFileName;
            if (previous is not null)
            {
                product.ClearImage();
                await uow.SaveChangesAsync(ct);
                await images.DeleteAsync(previous, ct);
            }

            return ProductDto.From(product, time.GetLocalNow().Date);
        }
    }
}
