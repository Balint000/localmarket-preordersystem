using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Tests;

/// <summary>Tesztadat-építők. A "most" rögzített: 2026-11-16, hétfő 10:00 (helyi idő).</summary>
internal static class TestData
{
    public static readonly DateTime Now = new(2026, 11, 16, 10, 0, 0);
    public static readonly DateTime Saturday = new(2026, 11, 21);
    /// <summary>Az átvétel napja, az idősáv (9–12) közben.</summary>
    public static readonly DateTime OnPickupDay = new(2026, 11, 21, 10, 0, 0);
    /// <summary>Az idősáv vége után.</summary>
    public static readonly DateTime AfterPickup = new(2026, 11, 21, 13, 0, 0);
    public static readonly TimeSpan Cutoff = TimeSpan.FromHours(12);

    public static Producer ApprovedProducer(DayOfWeek openOn = DayOfWeek.Saturday)
    {
        var market = Market.Create("Városi piac", "Fő tér 1", [openOn]);
        var producer = Producer.Register(market, "Kovács Gazdaság", 1);
        producer.Approve();
        return producer;
    }

    public static User Customer() => User.Register("Teszt", "Elek", "elek@example.com", "hash", Role.Customer, "+36301234567");

    public static Product ProductWithStock(Producer producer, decimal quantity = 10m, Units unit = Units.Kilogram, decimal price = 500m)
    {
        var product = Product.Create(producer, "Alma", "Friss alma", [Category.Create("Gyümölcs", "")]);
        product.AddWeeklyStock(Saturday, quantity, unit, price);
        return product;
    }

    public static PickupSlot Slot(Producer producer, int capacity = 2)
        => PickupSlot.Create(producer, Saturday, new TimeOnly(9, 0), new TimeOnly(12, 0), capacity);

    /// <summary>Egy tételes (2 kg alma) függő rendelés fizetéssel.</summary>
    public static (Order Order, Product Product, PickupSlot Slot) PendingOrder(decimal quantity = 2m)
    {
        var producer = ApprovedProducer();
        var product = ProductWithStock(producer);
        var slot = Slot(producer);
        var order = Order.Place(Customer(), slot, [(product, quantity)], Now);
        order.SetPayment(PaymentMethod.Cash);
        return (order, product, slot);
    }

    /// <summary>Lezárt (átvett) rendelés, amit utána vitatni lehet.</summary>
    public static (Order Order, Product Product, PickupSlot Slot) PickedUpOrder()
    {
        var data = PendingOrder();
        data.Order.Accept(Now);
        data.Order.MarkReady(Now);
        data.Order.MarkPickedUp(OnPickupDay);
        return data;
    }
}
