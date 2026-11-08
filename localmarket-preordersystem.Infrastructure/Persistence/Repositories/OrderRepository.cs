using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;
using localmarket_preordersystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace localmarket_preordersystem.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository(MarketDbContext context) : IOrderRepository
{
    private IQueryable<Order> ForUpdate() => context.Orders
        .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.WeeklyStocks)
        .Include(o => o.Payment)
        .Include(o => o.PickupSlot)
        .Include(o => o.User)
        .AsSplitQuery();

    // Csak olvasáshoz: a készlet nem kell.
    private IQueryable<Order> ForRead() => context.Orders.AsNoTracking()
        .Include(o => o.Items).ThenInclude(i => i.Product)
        .Include(o => o.Payment)
        .Include(o => o.PickupSlot)
        .Include(o => o.User)
        .AsSplitQuery();

    public Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => ForUpdate().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> ListByUserAsync(int userId, OrderStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = ForRead().Where(o => o.UserId == userId);
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);
        return await query.OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> ListByProducerAsync(int producerId, OrderStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = ForRead().Where(o => o.ProducerId == producerId);
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);
        return await query.OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> ListByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
        => await ForRead()
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> ListForPickingAsync(int producerId, DateTime pickupDate, CancellationToken cancellationToken = default)
    {
        var day = pickupDate.Date;
        return await ForRead()
            .Where(o => o.ProducerId == producerId
                        && o.PickupSlot.Date == day
                        && o.Status != OrderStatus.Cancelled
                        && o.Status != OrderStatus.Rejected)
            .OrderBy(o => o.PickupSlot.StartTime)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> AnyForPickupSlotAsync(int pickupSlotId, CancellationToken cancellationToken = default)
        => context.Orders.AnyAsync(o => o.PickupSlotId == pickupSlotId, cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
        => await context.Orders.AddAsync(order, cancellationToken);
}
