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
        else if (app.Environment.IsDevelopment())
        {
            log.LogWarning("Nincs EF migráció, EnsureCreated fut (csak Development).");
            await db.Database.EnsureCreatedAsync();
        }
        else
            throw new InvalidOperationException("Nincs EF migráció. Futtasd: dotnet ef migrations add InitialCreate.");

        if (!await db.Markets.AnyAsync())
        {
            var market = Market.Create("Városi Termelői Piac", "Fő tér 1", [DayOfWeek.Saturday]);
            market.UpdateLocation(46.8417, 16.8416);
            db.Markets.Add(market);
        }

        var hasher = sp.GetRequiredService<IPasswordHasher>();

        if (!await db.Users.AnyAsync(u => u.Role == Role.Admin))
        {
            var adminPw = ResolvePassword(cfg, "Seed:AdminPassword", "Admin123!", app.Environment);
            db.Users.Add(User.Register("Piac", "Vezető", "admin@localmarket.test",
                hasher.Hash(adminPw), Role.Admin, "+3612345678"));
        }

        if (app.Environment.IsDevelopment() && !await db.Users.AnyAsync(u => u.Email == "vevo@localmarket.test"))
        {
            var customerPw = ResolvePassword(cfg, "Seed:CustomerPassword", "Customer123!", app.Environment);
            db.Users.Add(User.Register("Teszt", "Vevő", "vevo@localmarket.test",
                hasher.Hash(customerPw), Role.Customer, "+3612345679"));
        }

        await db.SaveChangesAsync();
    }

    private static string ResolvePassword(IConfiguration cfg, string key, string devDefault, IHostEnvironment env)
    {
        var value = cfg[key];
        if (!string.IsNullOrWhiteSpace(value)) return value;
        if (env.IsDevelopment()) return devDefault;
        throw new InvalidOperationException($"A '{key}' konfiguráció hiányzik.");
    }
}
