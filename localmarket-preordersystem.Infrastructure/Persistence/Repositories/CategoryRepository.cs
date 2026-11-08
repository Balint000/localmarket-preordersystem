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

    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => context.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Category>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        return await context.Categories
            .Where(c => idList.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        // ILIKE escape-elt mintával: kis-nagybetűtől független, pontos egyezés
        var pattern = EscapeLike(name.Trim());
        return context.Categories.AnyAsync(
            c => EF.Functions.ILike(c.Name, pattern) && (excludeId == null || c.Id != excludeId),
            cancellationToken);
    }

    public Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default)
        => context.Products.AnyAsync(p => p.Categories.Any(c => c.Id == id), cancellationToken);

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
        => await context.Categories.AddAsync(category, cancellationToken);

    public void Remove(Category category) => context.Categories.Remove(category);

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
