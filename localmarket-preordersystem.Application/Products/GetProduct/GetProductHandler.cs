using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;
using localmarket_preordersystem.Application.Categories;

namespace localmarket_preordersystem.Application.Products.GetProduct
{
    public sealed class GetProductHandler(IProductRepository products, TimeProvider time)
    {
        public async Task<ProductDto> HandleAsync(int productId, CancellationToken ct = default)
        {
            var product = await products.GetByIdAsync(productId, ct)
                ?? throw new NotFoundException($"Nem található termék: {productId}.");
            return ProductDto.From(product, time.GetLocalNow().Date);
        }
    }
}
