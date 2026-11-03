using localmarket_preordersystem.Application.Common.Interfaces;

namespace localmarket_preordersystem.Application.Categories.ListCategories
{
    public sealed class ListCategoriesHandler(ICategoryRepository categories)
    {
        public async Task<IReadOnlyList<CategoryDto>> HandleAsync(CancellationToken ct = default)
            => (await categories.ListAsync(ct)).OrderBy(c => c.Name).Select(CategoryDto.From).ToList();
    }
}
