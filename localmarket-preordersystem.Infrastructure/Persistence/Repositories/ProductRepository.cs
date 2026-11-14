using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace localmarket_preordersystem.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(MarketDbContext context) : IProductRepository
{
    private IQueryable<Product> WithDetails() => context.Products
        .Include(p => p.Producer)
        .Include(p => p.Categories)
        .Include(p => p.WeeklyStocks)
        .AsSplitQuery();

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => WithDetails().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        return await WithDetails()
            .Where(p => idList.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(ProductSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var query = WithDetails().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(criteria.Text))
        {
            var pattern = $"%{EscapeLike(criteria.Text.Trim())}%";
            query = query.Where(p => EF.Functions.ILike(p.Name, pattern) || EF.Functions.ILike(p.Description, pattern));
        }

        if (criteria.CategoryId.HasValue)
            query = query.Where(p => p.Categories.Any(c => c.Id == criteria.CategoryId.Value));

        if (criteria.ProducerId.HasValue)
            query = query.Where(p => p.ProducerId == criteria.ProducerId.Value);

        var result = await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);

        if (criteria.ExcludeAllergies is { Count: > 0 })
            result = result.Where(p => !p.Allergies.Any(a => criteria.ExcludeAllergies.Contains(a))).ToList();

        return result;
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
        => await context.Products.AddAsync(product, cancellationToken);

    private static string EscapeLike(string value)
        => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
