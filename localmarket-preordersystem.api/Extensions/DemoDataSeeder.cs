using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;
using localmarket_preordersystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace localmarket_preordersystem.api.Extensions;

/// <summary>Bemutató mintaadatok (csak Development): jóváhagyott árusok, termékek, heti készlet, átvételi idősávok.</summary>
public static class DemoDataSeeder
{
    private record DemoProducer(string Email, string FirstName, string LastName, string StallName, int Stall);
    private record DemoProduct(int ProducerIndex, string Name, string Description, string Category, Allergy[] Allergies, decimal Quantity, Units Unit, decimal Price);

    public static async Task SeedAsync(MarketDbContext db, IPasswordHasher hasher, string password)
    {
        if (await db.Producers.AnyAsync()) return;

        var market = await db.Markets.FirstAsync();
        var categories = await db.Categories.ToDictionaryAsync(c => c.Name);
        var nextMarketDay = NextOpeningDay(market, DateTime.Today);

        var producerData = new[]
        {
            new DemoProducer("kovacs.gazdasag@localmarket.test", "Péter", "Kovács", "Kovács Gazdaság", 1),
            new DemoProducer("szabo.pekseg@localmarket.test", "Anna", "Szabó", "Szabó Pékség", 2),
            new DemoProducer("nagy.sajt@localmarket.test", "Gábor", "Nagy", "Nagy Sajtműhely", 3),
        };

        var producers = new List<Producer>();
        foreach (var d in producerData)
        {
            var user = User.Register(d.FirstName, d.LastName, d.Email, hasher.Hash(password), Role.Producer, "+36301234567");
            var producer = Producer.Register(market, d.StallName, d.Stall);
            producer.Approve();
            user.AttachProducerProfile(producer);
            db.Users.Add(user);
            producers.Add(producer);
        }
        await db.SaveChangesAsync();

        var products = new[]
        {
            new DemoProduct(0, "Paradicsom", "Szezonális, napon érlelt", "Zöldség", [], 40, Units.Kilogram, 900),
            new DemoProduct(0, "Alma", "Házi, vegyszermentes", "Gyümölcs", [], 60, Units.Kilogram, 450),
            new DemoProduct(1, "Kovászos kenyér", "1 kg-os, kézzel formázott", "Pékáru", [Allergy.Gluten], 30, Units.Piece, 1400),
            new DemoProduct(1, "Sajtos pogácsa", "10 db-os csomag", "Pékáru", [Allergy.Gluten, Allergy.Milk, Allergy.Eggs], 20, Units.Pack, 1800),
            new DemoProduct(2, "Trappista sajt", "Félkemény, érlelt", "Tejtermék", [Allergy.Milk], 15, Units.Kilogram, 4200),
            new DemoProduct(2, "Natúr joghurt", "500 ml-es üveg", "Tejtermék", [Allergy.Milk], 25, Units.Bottle, 700),
        };

        foreach (var p in products)
        {
            var product = Product.Create(producers[p.ProducerIndex], p.Name, p.Description, [categories[p.Category]], p.Allergies);
            product.AddWeeklyStock(nextMarketDay, p.Quantity, p.Unit, p.Price);
            db.Products.Add(product);
        }

        foreach (var producer in producers)
        {
            db.PickupSlots.Add(PickupSlot.Create(producer, nextMarketDay, new TimeOnly(8, 0), new TimeOnly(10, 0), 10));
            db.PickupSlots.Add(PickupSlot.Create(producer, nextMarketDay, new TimeOnly(10, 0), new TimeOnly(12, 0), 10));
        }

        await db.SaveChangesAsync();
    }

    private static DateTime NextOpeningDay(Market market, DateTime today)
    {
        for (var i = 1; i <= 7; i++)
        {
            var day = today.Date.AddDays(i);
            if (market.OpeningDays.Contains(day.DayOfWeek)) return day;
        }
        return today.Date.AddDays(1);
    }
}
