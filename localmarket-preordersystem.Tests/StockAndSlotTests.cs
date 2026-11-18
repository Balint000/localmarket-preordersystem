using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.Exceptions;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Tests;

public class ProductStockTests
{
    private static ProductStock Stock(decimal quantity, Units unit)
    {
        var product = TestData.ProductWithStock(TestData.ApprovedProducer(), quantity, unit);
        return product.GetStockForWeek(TestData.Saturday)!;
    }

    [Fact]
    public void Week_start_is_normalized_to_monday()
    {
        Assert.Equal(new DateTime(2026, 11, 16), ProductStock.GetWeekStart(new DateTime(2026, 11, 22))); // vasárnap
        Assert.Equal(new DateTime(2026, 11, 16), ProductStock.GetWeekStart(new DateTime(2026, 11, 16))); // hétfő
        Assert.Equal(new DateTime(2026, 11, 16), ProductStock.GetWeekStart(TestData.Saturday));
    }

    [Fact]
    public void Zero_quantity_means_sold_out_and_is_allowed()
    {
        var stock = Stock(0m, Units.Kilogram);
        Assert.Equal(0m, stock.AvailableQuantity);
    }

    [Fact]
    public void Negative_quantity_and_non_positive_price_are_rejected()
    {
        var product = Product.Create(TestData.ApprovedProducer(), "Körte", "", [Category.Create("Gyümölcs", "")]);
        Assert.Throws<DomainException>(() => product.AddWeeklyStock(TestData.Saturday, -1m, Units.Kilogram, 100m));
        Assert.Throws<DomainException>(() => product.AddWeeklyStock(TestData.Saturday, 1m, Units.Kilogram, 0m));
    }

    [Fact]
    public void Reserve_cannot_exceed_the_available_quantity()
    {
        var stock = Stock(5m, Units.Kilogram);
        stock.Reserve(5m);
        Assert.Equal(0m, stock.AvailableQuantity);
        Assert.Throws<DomainException>(() => stock.Reserve(0.5m));
    }

    [Fact]
    public void Release_never_goes_below_zero()
    {
        var stock = Stock(5m, Units.Kilogram);
        stock.Reserve(2m);
        stock.Release(10m);
        Assert.Equal(0m, stock.ReservedQuantity);
    }

    [Fact]
    public void Weight_units_accept_fractions_up_to_three_decimals()
    {
        var stock = Stock(5m, Units.Kilogram);
        stock.Reserve(0.25m);
        stock.Reserve(0.125m);
        Assert.Throws<DomainException>(() => stock.Reserve(0.0005m));
    }

    [Fact]
    public void Countable_units_accept_only_whole_quantities()
    {
        var stock = Stock(10m, Units.Piece);
        stock.Reserve(2m);
        Assert.Throws<DomainException>(() => stock.Reserve(1.5m));
    }

    [Theory]
    [InlineData(Units.Piece, true)]
    [InlineData(Units.Bundle, true)]
    [InlineData(Units.Jar, true)]
    [InlineData(Units.Kilogram, false)]
    [InlineData(Units.Gram, false)]
    [InlineData(Units.Decagram, false)]
    [InlineData(Units.Litre, false)]
    [InlineData(Units.Millilitre, false)]
    public void IsCountable_matches_the_unit_kind(Units unit, bool expected)
        => Assert.Equal(expected, unit.IsCountable());

    [Fact]
    public void Ordering_in_a_week_without_stock_fails()
    {
        var product = Product.Create(TestData.ApprovedProducer(), "Körte", "", [Category.Create("Gyümölcs", "")]);
        Assert.Throws<DomainException>(() => product.ReserveStockForPickup(TestData.Saturday, 1m));
    }
}

public class PickupSlotTests
{
    [Fact]
    public void Slot_can_only_be_created_on_an_opening_day()
    {
        var producer = TestData.ApprovedProducer(DayOfWeek.Saturday);
        var sunday = TestData.Saturday.AddDays(1);
        Assert.Throws<DomainException>(() => PickupSlot.Create(producer, sunday, new TimeOnly(9, 0), new TimeOnly(10, 0), 3));
    }

    [Fact]
    public void Slot_requires_a_valid_time_range_and_capacity()
    {
        var producer = TestData.ApprovedProducer();
        Assert.Throws<DomainException>(() => PickupSlot.Create(producer, TestData.Saturday, new TimeOnly(10, 0), new TimeOnly(10, 0), 3));
        Assert.Throws<DomainException>(() => PickupSlot.Create(producer, TestData.Saturday, new TimeOnly(9, 0), new TimeOnly(10, 0), 0));
    }

    [Fact]
    public void Booking_stops_at_capacity_and_release_frees_a_seat()
    {
        var slot = TestData.Slot(TestData.ApprovedProducer(), capacity: 1);
        slot.Book(TestData.Now);
        Assert.False(slot.HasFreeCapacity());
        Assert.Throws<DomainException>(() => slot.Book(TestData.Now));

        slot.Release();
        Assert.True(slot.HasFreeCapacity());
        slot.Release(); // nem megy 0 alá
        Assert.Equal(0, slot.BookedCount);
    }

    [Fact]
    public void An_ended_slot_cannot_be_booked()
    {
        var slot = TestData.Slot(TestData.ApprovedProducer());
        var afterEnd = TestData.Saturday.AddHours(12).AddMinutes(1);
        Assert.True(slot.HasEnded(afterEnd));
        Assert.Throws<DomainException>(() => slot.Book(afterEnd));
    }

    [Fact]
    public void A_booked_slot_cannot_be_removed_or_moved()
    {
        var slot = TestData.Slot(TestData.ApprovedProducer(), capacity: 3);
        slot.Book(TestData.Now);

        Assert.Throws<DomainException>(() => slot.EnsureCanBeRemoved());
        Assert.Throws<DomainException>(() => slot.Update(TestData.Saturday, new TimeOnly(10, 0), new TimeOnly(13, 0), 3, TestData.Now));
        Assert.Throws<DomainException>(() => slot.Update(TestData.Saturday, new TimeOnly(9, 0), new TimeOnly(12, 0), 0, TestData.Now));
    }
}

public class RegistrationRuleTests
{
    [Fact]
    public void Producer_cannot_register_at_an_inactive_market()
    {
        var market = Market.Create("Piac", "Cím", [DayOfWeek.Saturday]);
        market.Deactivate();
        Assert.Throws<DomainException>(() => Producer.Register(market, "Gazda", 1));
    }

    [Fact]
    public void Unapproved_producer_cannot_list_products()
    {
        var market = Market.Create("Piac", "Cím", [DayOfWeek.Saturday]);
        var producer = Producer.Register(market, "Gazda", 1); // Pending
        Assert.Throws<DomainException>(() => Product.Create(producer, "Alma", "", [Category.Create("Gyümölcs", "")]));
    }

    [Fact]
    public void Payment_methods_do_not_contain_the_joke_entry()
        => Assert.DoesNotContain("DishWashing", Enum.GetNames<PaymentMethod>());
}
