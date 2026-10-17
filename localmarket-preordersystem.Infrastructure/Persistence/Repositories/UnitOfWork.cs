using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Infrastructure.Persistence.Context;

namespace localmarket_preordersystem.Infrastructure.Persistence.Repositories;

internal sealed class UnitOfWork(MarketDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}