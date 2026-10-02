using localmarket_preordersystem.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace localmarket_preordersystem.Infrastructure.Persistence.Context;

public sealed class MarketDbContext(DbContextOptions<MarketDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Market> Markets => Set<Market>();
    public DbSet<Producer> Producers => Set<Producer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductStock> ProductStocks => Set<ProductStock>();
    public DbSet<PickupSlot> PickupSlots => Set<PickupSlot>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Logs> AuditLogs => Set<Logs>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MarketDbContext).Assembly);

        // This type currently duplicates the OrderItemStatusEntity and is not part of the model.
        modelBuilder.Ignore<localmarket_preordersystem.Domain.Entity.OrderItemStatusEntity>();
    }
}