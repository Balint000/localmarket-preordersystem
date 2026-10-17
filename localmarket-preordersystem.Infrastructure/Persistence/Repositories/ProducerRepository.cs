using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;
using localmarket_preordersystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace localmarket_preordersystem.Infrastructure.Persistence.Repositories;

internal sealed class ProducerRepository(MarketDbContext context) : IProducerRepository
{
    public Task<Producer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => context.Producers.Include(p => p.Market)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Producer>> ListAsync(ProducerStatus? status = null, int? marketId = null, CancellationToken cancellationToken = default)
    {
        var query = context.Producers.AsNoTracking().Include(p => p.Market).AsQueryable();

        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        if (marketId.HasValue) query = query.Where(p => p.MarketId == marketId.Value);

        return await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Producer producer, CancellationToken cancellationToken = default)
        => await context.Producers.AddAsync(producer, cancellationToken);
}