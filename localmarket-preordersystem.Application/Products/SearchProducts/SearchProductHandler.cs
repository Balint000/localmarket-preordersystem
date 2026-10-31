using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;
using localmarket_preordersystem.Application.Categories;

namespace localmarket_preordersystem.Application.Products.SearchProducts
{
    /// <summary>Szabadszöveges, kategória-, árus- és allergén-kizáró keresés. Nyilvános.</summary>
    public sealed record SearchProductsQuery(string? Text = null, int? CategoryId = null, int? ProducerId = null, IReadOnlyList<Allergy>? ExcludeAllergies = null);

    public sealed class SearchProductsHandler(IProductRepository products, TimeProvider time)
    {
        public async Task<IReadOnlyList<ProductDto>> HandleAsync(SearchProductsQuery query, CancellationToken ct = default)
        {
            var criteria = new ProductSearchCriteria(query.Text?.Trim(), query.CategoryId, query.ProducerId, query.ExcludeAllergies);
            var today = time.GetLocalNow().Date;
            return (await products.SearchAsync(criteria, ct)).Select(p => ProductDto.From(p, today)).ToList();
        }
    }
}
