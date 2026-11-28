using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Tests;

/// <summary>Egyszerű, memóriabeli tesztdublőrök a handler-tesztekhez (adatbázis nélkül).</summary>
internal static class Fakes
{
    /// <summary>Az entitások Id-ja privát setterű (az adatbázis adja); a tesztekben reflexióval állítjuk be.</summary>
    public static T WithId<T>(this T entity, int id) where T : class
    {
        typeof(T).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }
}

internal sealed class FakeCurrentUser(int? userId, Role? role, int? producerId = null) : ICurrentUserService
{
    public bool IsAuthenticated => UserId is not null;
    public int? UserId { get; } = userId;
    public Role? Role { get; } = role;
    public int? ProducerId { get; } = producerId;
}

internal sealed class FixedTimeProvider(DateTime localNow) : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(localNow, DateTimeKind.Utc));
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(0);
    }
}

internal sealed class FakeAuditLog : IAuditLogRepository
{
    public List<Logs> Entries { get; } = new();

    public Task AddAsync(Logs log, CancellationToken cancellationToken = default)
    {
        Entries.Add(log);
        return Task.CompletedTask;
    }

    public Task<PagedList<Logs>> ListAsync(string? entityName, int? entityId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var filtered = Entries
            .Where(l => entityName is null || l.EntityName == entityName)
            .Where(l => entityId is null || l.EntityId == entityId)
            .OrderByDescending(l => l.CreatedAt)
            .ToList();
        return Task.FromResult(new PagedList<Logs>(filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList(), filtered.Count));
    }
}

internal sealed class FakeUserRepository(params User[] seed) : IUserRepository
{
    private readonly List<User> _users = seed.ToList();

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<UserAccessInfo?> GetAccessInfoAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_users.Where(u => u.Id == id).Select(u => new UserAccessInfo(u.IsActive, u.Role, u.ProducerId)).FirstOrDefault());

    public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_users.Count(u => u.Role == Role.Admin && u.IsActive));

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => Task.FromResult(_users.FirstOrDefault(u => u.Email == email.Trim().ToLowerInvariant()));

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        => Task.FromResult(_users.Any(u => u.Email == email.Trim().ToLowerInvariant()));

    public Task<IReadOnlyList<User>> ListAsync(Role? role = null, bool? isActive = null, string? search = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<User>>(_users
            .Where(u => role is null || u.Role == role)
            .Where(u => isActive is null || u.IsActive == isActive)
            .ToList());

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }
}

internal sealed class FakeProducerRepository(params Producer[] seed) : IProducerRepository
{
    private readonly List<Producer> _producers = seed.ToList();

    public Task<Producer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_producers.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Producer>> ListAsync(ProducerStatus? status = null, int? marketId = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Producer>>(_producers
            .Where(p => status is null || p.Status == status)
            .Where(p => marketId is null || p.MarketId == marketId)
            .ToList());

    public Task<IReadOnlyList<Producer>> ListByMarketForUpdateAsync(int marketId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Producer>>(_producers.Where(p => p.MarketId == marketId).OrderBy(p => p.Id).ToList());

    public Task AddAsync(Producer producer, CancellationToken cancellationToken = default)
    {
        _producers.Add(producer);
        return Task.CompletedTask;
    }
}

internal sealed class FakeMarketRepository(params Market[] seed) : IMarketRepository
{
    private readonly List<Market> _markets = seed.ToList();

    public Task<Market?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_markets.FirstOrDefault(m => m.Id == id));

    public Task<IReadOnlyList<Market>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Market>>(_markets.ToList());

    public Task AddAsync(Market market, CancellationToken cancellationToken = default)
    {
        _markets.Add(market);
        return Task.CompletedTask;
    }
}

internal sealed class FakePickupSlotRepository(params PickupSlot[] seed) : IPickupSlotRepository
{
    public List<PickupSlot> Slots { get; } = seed.ToList();

    public Task<PickupSlot?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(Slots.FirstOrDefault(s => s.Id == id));

    public Task<PickupSlot?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<PickupSlot>> ListByProducerAsync(int producerId, DateTime? fromDate = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PickupSlot>>(Slots
            .Where(s => s.ProducerId == producerId && (fromDate is null || s.Date >= fromDate.Value.Date))
            .ToList());

    public Task AddAsync(PickupSlot slot, CancellationToken cancellationToken = default)
    {
        Slots.Add(slot);
        return Task.CompletedTask;
    }

    public void Remove(PickupSlot slot) => Slots.Remove(slot);
}

internal sealed class FakeOrderRepository(params Order[] seed) : IOrderRepository
{
    public List<Order> Orders { get; } = seed.ToList();

    public Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(Orders.FirstOrDefault(o => o.Id == id));

    public Task<IReadOnlyList<Order>> ListByUserAsync(int userId, OrderStatus? status = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Order>>(Orders.Where(o => o.UserId == userId && (status is null || o.Status == status)).ToList());

    public Task<IReadOnlyList<Order>> ListByProducerAsync(int producerId, OrderStatus? status = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Order>>(Orders.Where(o => o.ProducerId == producerId && (status is null || o.Status == status)).ToList());

    public Task<IReadOnlyList<Order>> ListByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Order>>(Orders.Where(o => o.Status == status).ToList());

    public Task<IReadOnlyList<Order>> ListForPickingAsync(int? producerId, DateTime pickupDate, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Order>>(Orders
            .Where(o => (producerId is null || o.ProducerId == producerId) && o.PickupSlot.Date == pickupDate.Date)
            .Where(o => o.Status is not (OrderStatus.Cancelled or OrderStatus.Rejected or OrderStatus.Expired))
            .ToList());

    public Task<IReadOnlyList<Order>> ListPendingWithEndedSlotAsync(DateTime localNow, int take, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Order>>(Orders
            .Where(o => o.Status == OrderStatus.Pending && o.PickupSlot.HasEnded(localNow))
            .Take(take)
            .ToList());

    public Task<IReadOnlyList<Order>> ListActiveContainingProductAsync(int productId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Order>>(Orders
            .Where(o => o.Status is OrderStatus.Pending or OrderStatus.Accepted or OrderStatus.PartiallyFulfilled or OrderStatus.Ready
                        && o.Items.Any(i => i.ProductId == productId))
            .ToList());

    public Task<bool> AnyForPickupSlotAsync(int pickupSlotId, CancellationToken cancellationToken = default)
        => Task.FromResult(Orders.Any(o => o.PickupSlotId == pickupSlotId));

    public Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        Orders.Add(order);
        return Task.CompletedTask;
    }
}

internal sealed class FakeProductRepository(params Product[] seed) : IProductRepository
{
    private readonly List<Product> _products = seed.ToList();

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_products.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var set = ids.ToHashSet();
        return Task.FromResult<IReadOnlyList<Product>>(_products.Where(p => set.Contains(p.Id)).ToList());
    }

    public Task<IReadOnlyList<Product>> SearchAsync(ProductSearchCriteria criteria, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Product>>(_products.ToList());

    /// <summary>A teszt állítja: van-e rendelési előzmény (archiválás vagy fizikai törlés).</summary>
    public bool HasOrderHistory { get; set; }
    public List<Product> Removed { get; } = new();

    public Task<bool> HasOrdersAsync(int productId, CancellationToken cancellationToken = default)
        => Task.FromResult(HasOrderHistory);

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _products.Add(product);
        return Task.CompletedTask;
    }

    public void Remove(Product product)
    {
        _products.Remove(product);
        Removed.Add(product);
    }
}

internal sealed class FakeCartRepository : ICartRepository
{
    public List<int> ClearedProducts { get; } = new();

    public Task<Cart?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult<Cart?>(null);
    public Task AddAsync(Cart cart, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RemoveItemsForProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        ClearedProducts.Add(productId);
        return Task.CompletedTask;
    }
}

internal sealed class FakeImageStorage : IImageStorage
{
    public Task<string> SaveAsync(byte[] content, string extension, CancellationToken cancellationToken = default) => Task.FromResult("x" + extension);
    public Task DeleteAsync(string fileName, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
