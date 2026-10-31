using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;
using localmarket_preordersystem.Application.Categories;

namespace localmarket_preordersystem.Application.Products.UpdateProduct
{
    public sealed class UpdateProductHandler(
        IProductRepository products,
        ICategoryRepository categories,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        TimeProvider time)
    {
        public async Task<ProductDto> HandleAsync(UpdateProductCommand command, CancellationToken ct = default)
        {
            var producerId = currentUser.RequireProducerId();
            UpdateProductValidator.Validate(command);

            var product = await products.GetByIdAsync(command.ProductId, ct)
                ?? throw new NotFoundException($"Nem található termék: {command.ProductId}.");
            if (product.ProducerId != producerId)
                throw new ForbiddenAccessException("Csak a saját termékedet módosíthatod.");

            var ids = command.CategoryIds.Distinct().ToList();
            var found = await categories.GetByIdsAsync(ids, ct);
            if (found.Count != ids.Count)
                throw new NotFoundException("Egy vagy több megadott kategória nem létezik.");

            product.UpdateDetails(command.Name, command.Description ?? string.Empty, found, command.Allergies ?? Array.Empty<Allergy>());
            await uow.SaveChangesAsync(ct);
            return ProductDto.From(product, time.GetLocalNow().Date);
        }
    }
}
