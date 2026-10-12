using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Common.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> ListByUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> ListByProducerAsync(int producerId, OrderStatus? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> ListByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
}