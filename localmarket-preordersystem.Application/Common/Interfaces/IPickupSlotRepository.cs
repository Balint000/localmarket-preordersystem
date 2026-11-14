using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Common.Interfaces;

public interface IPickupSlotRepository
{
    Task<PickupSlot?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Módosításhoz: betöltve a Producer és annak Market-je (nyitvatartási nap ellenőrzés).</summary>
    Task<PickupSlot?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PickupSlot>> ListByProducerAsync(int producerId, DateTime? fromDate = null, CancellationToken cancellationToken = default);
    Task AddAsync(PickupSlot slot, CancellationToken cancellationToken = default);
    void Remove(PickupSlot slot);
}
