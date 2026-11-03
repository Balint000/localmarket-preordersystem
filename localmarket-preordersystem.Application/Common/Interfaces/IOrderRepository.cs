using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Common.Interfaces;

public interface IOrderRepository
{
    /// <summary>Betöltve: Items, PickupSlot, Payment (és a tételek Product-ja a készlet-felszabadításhoz).</summary>
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> ListByUserAsync(int userId, OrderStatus? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> ListByProducerAsync(int producerId, OrderStatus? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> ListByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
    /// <summary>Az árus aktív (nem lemondott/elutasított) rendelései egy átvételi napra — csomagolólista.</summary>
    Task<IReadOnlyList<Order>> ListForPickingAsync(int producerId, DateTime pickupDate, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
}
