using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace localmarket_preordersystem.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(MarketDbContext context) : IProductRepository
{
    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => context.Products
            .Include(p => p.Producer)
            .Include(p => p.Categories)
            .Include(p => p.WeeklyStocks)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> SearchAsync(string? text = null, int? categoryId = null, int? producerId = null, CancellationToken cancellationToken = default)
    {
        var query = context.Products.AsNoTracking()
            .Include(p => p.Producer)
            .Include(p => p.Categories)
            .Include(p => p.WeeklyStocks)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(text))
        {
            var pattern = $"%{EscapeLike(text.Trim())}%";
            query = query.Where(p => EF.Functions.ILike(p.Name, pattern)
                                     || EF.Functions.ILike(p.Description, pattern));
        }

        if (categoryId.HasValue)
            query = query.Where(p => p.Categories.Any(c => c.Id == categoryId.Value));

        if (producerId.HasValue)
            query = query.Where(p => p.ProducerId == producerId.Value);

        return await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
        => await context.Products.AddAsync(product, cancellationToken);

    private static string EscapeLike(string value)
        => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}