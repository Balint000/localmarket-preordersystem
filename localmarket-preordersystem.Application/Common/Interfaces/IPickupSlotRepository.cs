using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Common.Interfaces;

public interface IPickupSlotRepository
{
    Task<PickupSlot?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PickupSlot>> ListByProducerAsync(int producerId, DateTime? fromDate = null, CancellationToken cancellationToken = default);
    Task AddAsync(PickupSlot slot, CancellationToken cancellationToken = default);
}