using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Infrastructure;

public class PreorderDbContext : DbContext
{
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Logs> Logs => Set<Logs>();
    public DbSet<Market> Markets => Set<Market>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemStatus> OrderItemsStatus => Set<OrderItemStatus>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PickupSlot> PickupSlots => Set<PickupSlot>();
    public DbSet<Producer> Producers => Set<Producer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductStock> ProductStocks => Set<ProductStock>();
    public DbSet<User> Users => Set<User>();

    public PreorderDbContext(DbContextOptions<PreorderDbContext> options) : base(options) {}
}