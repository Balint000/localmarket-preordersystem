using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Validation;

namespace localmarket_preordersystem.Application.Products.SetProductImage
{
    public sealed record SetProductImageCommand(int ProductId, byte[] Content);

    /// <summary>
    /// Termékkép feltöltése / cseréje — csak a saját termékhez. A típust a fájl tartalma dönti el
    /// (JPEG, PNG, WebP), a méret legfeljebb 2 MB. A korábbi kép törlődik.
    /// </summary>
    public sealed class SetProductImageHandler(
        IProductRepository products,
        IImageStorage images,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        TimeProvider time)
    {
        public async Task<ProductDto> HandleAsync(SetProductImageCommand command, CancellationToken ct = default)
        {
            var producerId = currentUser.RequireProducerId();

            new ValidationBuilder()
                .Positive(nameof(command.ProductId), command.ProductId, "Érvénytelen termék azonosító.")
                .Must(nameof(command.Content), command.Content is { Length: > 0 }, "A kép megadása kötelező.")
                .Must(nameof(command.Content), command.Content is null || command.Content.Length <= ProductImages.MaxBytes,
                    $"A kép legfeljebb {ProductImages.MaxBytes / (1024 * 1024)} MB lehet.")
                .ThrowIfInvalid();

            var extension = ProductImages.DetectExtension(command.Content)
                ?? throw new ValidationException(new Dictionary<string, string[]>
                {
                    [nameof(command.Content)] = new[] { "Csak JPEG, PNG vagy WebP kép tölthető fel." }
                });

            var product = await products.GetByIdAsync(command.ProductId, ct)
                ?? throw new NotFoundException($"Nem található termék: {command.ProductId}.");
            if (product.ProducerId != producerId)
                throw new ForbiddenAccessException("Csak a saját terméked képét módosíthatod.");

            var previous = product.ImageFileName;
            var fileName = await images.SaveAsync(command.Content, extension, ct);
            product.SetImage(fileName);

            try
            {
                await uow.SaveChangesAsync(ct);
            }
            catch
            {
                // sikertelen mentésnél ne maradjon árva fájl
                await images.DeleteAsync(fileName, CancellationToken.None);
                throw;
            }

            if (previous is not null)
                await images.DeleteAsync(previous, ct);

            return ProductDto.From(product, time.GetLocalNow().Date);
        }
    }
}
