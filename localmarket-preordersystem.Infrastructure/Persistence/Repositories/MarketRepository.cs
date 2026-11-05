using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace localmarket_preordersystem.Infrastructure.Persistence.Repositories;

internal sealed class MarketRepository(MarketDbContext context) : IMarketRepository
{
    public Task<Market?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => context.Markets.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
}