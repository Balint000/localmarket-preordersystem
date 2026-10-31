using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;
using localmarket_preordersystem.Application.Categories;

namespace localmarket_preordersystem.Application.Products.CreateProduct
{
    /// <summary>Új termék — csak jóváhagyott árus, és csak a saját nevében (a ProducerId a tokenből jön).</summary>
    public sealed class CreateProductHandler(
        IProductRepository products,
        IProducerRepository producers,
        ICategoryRepository categories,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        TimeProvider time)
    {
        public async Task<ProductDto> HandleAsync(CreateProductCommand command, CancellationToken ct = default)
        {
            var producerId = currentUser.RequireProducerId();
            CreateProductValidator.Validate(command);

            var producer = await producers.GetByIdAsync(producerId, ct)
                ?? throw new NotFoundException("Az árus-profil nem található.");

            var ids = command.CategoryIds.Distinct().ToList();
            var found = await categories.GetByIdsAsync(ids, ct);
            if (found.Count != ids.Count)
                throw new NotFoundException("Egy vagy több megadott kategória nem létezik.");

            var product = Product.Create(producer, command.Name, command.Description ?? string.Empty, found, command.Allergies);
            await products.AddAsync(product, ct);
            await uow.SaveChangesAsync(ct);
            return ProductDto.From(product, time.GetLocalNow().Date);
        }
    }
}
