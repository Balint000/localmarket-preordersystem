using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;
using localmarket_preordersystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace localmarket_preordersystem.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository(MarketDbContext context) : IOrderRepository
{
    private IQueryable<Order> WithDetails() => context.Orders
        .Include(o => o.Items).ThenInclude(i => i.Product)
        .Include(o => o.Payment)
        .Include(o => o.PickupSlot);

    public Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => WithDetails().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> ListByUserAsync(int userId, CancellationToken cancellationToken = default)
        => await WithDetails().AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> ListByProducerAsync(int producerId, OrderStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = WithDetails().AsNoTracking()
            .Where(o => o.Items.Any(i => i.Product.ProducerId == producerId));

        if (status.HasValue) query = query.Where(o => o.Status == status.Value);

        return await query.OrderByDescending(o => o.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> ListByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
        => await WithDetails().AsNoTracking()
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
        => await context.Orders.AddAsync(order, cancellationToken);
}