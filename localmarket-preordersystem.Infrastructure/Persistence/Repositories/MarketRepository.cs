using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace localmarket_preordersystem.Infrastructure.Persistence.Repositories;

internal sealed class MarketRepository(MarketDbContext context) : IMarketRepository
{
    public Task<Market?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => context.Markets.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Market>> ListAsync(CancellationToken cancellationToken = default)
        => await context.Markets.AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Market market, CancellationToken cancellationToken = default)
        => await context.Markets.AddAsync(market, cancellationToken);
}
