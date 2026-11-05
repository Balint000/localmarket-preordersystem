using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace localmarket_preordersystem.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository(MarketDbContext context) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default)
        => await context.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Category>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        return await context.Categories
            .Where(c => idList.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
        => await context.Categories.AddAsync(category, cancellationToken);
}