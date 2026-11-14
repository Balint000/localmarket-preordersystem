using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;
using localmarket_preordersystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace localmarket_preordersystem.api.Extensions;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<MarketDbContext>();
        var cfg = sp.GetRequiredService<IConfiguration>();
        var log = sp.GetRequiredService<ILogger<MarketDbContext>>();

        if (db.Database.GetMigrations().Any())
            await db.Database.MigrateAsync();
        else
        {
            log.LogWarning("Nincs EF migráció, EnsureCreated fut.");
            await db.Database.EnsureCreatedAsync();
        }

        if (!await db.Markets.AnyAsync())
        {
            var market = Market.Create("Városi Termelői Piac", "Fő tér 1", [DayOfWeek.Saturday]);
            market.UpdateLocation(46.8417, 16.8416);
            db.Markets.Add(market);
        }

        var hasher = sp.GetRequiredService<IPasswordHasher>();

        if (!await db.Users.AnyAsync(u => u.Role == Role.Admin))
        {
            db.Users.Add(User.Register("Piac", "Vezető", "admin@localmarket.test",
                hasher.Hash(cfg["Seed:AdminPassword"] ?? "Admin123!"), Role.Admin, "+3612345678"));
        }

        if (!await db.Users.AnyAsync(u => u.Email == "vevo@localmarket.test"))
        {
            db.Users.Add(User.Register("Teszt", "Vevő", "vevo@localmarket.test",
                hasher.Hash(cfg["Seed:CustomerPassword"] ?? "Customer123!"), Role.Customer, "+3612345679"));
        }

        await db.SaveChangesAsync();
    }
}
