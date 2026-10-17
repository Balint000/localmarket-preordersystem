using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace localmarket_preordersystem.Infrastructure.Persistence.Repositories;

internal sealed class PickupSlotRepository(MarketDbContext context) : IPickupSlotRepository
{
    public Task<PickupSlot?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => context.PickupSlots.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PickupSlot>> ListByProducerAsync(int producerId, DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        var query = context.PickupSlots.AsNoTracking().Where(s => s.ProducerId == producerId);

        if (fromDate.HasValue)
            query = query.Where(s => s.Date >= fromDate.Value.Date);

        return await query.OrderBy(s => s.Date).ThenBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PickupSlot slot, CancellationToken cancellationToken = default)
        => await context.PickupSlots.AddAsync(slot, cancellationToken);
}