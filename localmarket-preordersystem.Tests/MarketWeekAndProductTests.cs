using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Producers.StockReview;
using localmarket_preordersystem.Application.Products.DeleteProduct;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.Exceptions;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Tests;

/// <summary>Nyitvatartási órák, a hét lezárása (a heti kínálat rendelhetősége), archiválás, admin termék-törlés.</summary>
public class MarketHoursTests
{
    [Fact]
    public void Closing_time_must_be_after_opening()
    {
        var market = Market.Create("Piac", "Fő tér 1", [DayOfWeek.Saturday]);
        Assert.Equal(Market.DefaultOpensAt, market.OpensAt);
        Assert.Throws<DomainException>(() => market.SetOpeningHours(new TimeOnly(13, 0), new TimeOnly(7, 0)));
    }

    [Fact]
    public void Slot_must_be_within_the_opening_hours()
    {
        var producer = TestData.ApprovedProducer(); // 07:00–13:00
        Assert.Throws<DomainException>(() => PickupSlot.Create(producer, TestData.Saturday, new TimeOnly(12, 0), new TimeOnly(14, 0), 3));
        Assert.Throws<DomainException>(() => PickupSlot.Create(producer, TestData.Saturday, new TimeOnly(6, 0), new TimeOnly(8, 0), 3));
        PickupSlot.Create(producer, TestData.Saturday, new TimeOnly(7, 0), new TimeOnly(13, 0), 3);
    }

    [Fact]
    public void Narrowed_hours_make_an_existing_slot_unbookable()
    {
        var producer = TestData.ApprovedProducer();
        var slot = TestData.Slot(producer); // 9–12
        producer.Market.SetOpeningHours(new TimeOnly(7, 0), new TimeOnly(11, 0));
        Assert.False(slot.IsBookable(TestData.Now));
        Assert.Throws<DomainException>(() => slot.Book(TestData.Now));
    }

    [Theory]
    [InlineData(2026, 11, 16, 10, true)]   // hétfő: szombat még előttünk
    [InlineData(2026, 11, 21, 12, true)]   // szombat zárás (13:00) előtt
    [InlineData(2026, 11, 21, 13, false)]  // szombat zárás után
    [InlineData(2026, 11, 22, 9, false)]   // vasárnap: a héten nincs több piacnap
    public void Week_is_open_until_the_last_market_day_closes(int y, int m, int d, int h, bool expected)
    {
        var market = Market.Create("Piac", "Fő tér 1", [DayOfWeek.Saturday]);
        Assert.Equal(expected, market.HasOpeningLeftInWeek(new DateTime(y, m, d, h, 0, 0)));
    }
}

public class WeeklyOfferTests
{
    [Fact]
    public void Current_week_offer_is_not_orderable_after_the_last_market_day()
    {
        var producer = TestData.ApprovedProducer();
        var product = TestData.ProductWithStock(producer); // a 2026-11-16-i hét kínálata

        Assert.NotNull(product.GetNextAvailableStock(TestData.OnPickupDay));
        Assert.Null(product.GetNextAvailableStock(TestData.AfterPickup));            // szombat 13:00 után
        Assert.Single(product.GetClosedWeekStocks(TestData.AfterPickup));
    }

    [Fact]
    public void Next_week_offer_takes_over_when_the_current_week_closed()
    {
        var producer = TestData.ApprovedProducer();
        var product = TestData.ProductWithStock(producer);
        product.AddWeeklyStock(TestData.Saturday.AddDays(7), 8m, Units.Kilogram, 550m);

        var next = product.GetNextAvailableStock(TestData.AfterPickup);
        Assert.Equal(TestData.Saturday.AddDays(7).AddDays(-5), next!.WeekStartDate);
        Assert.Equal(550m, next.UnitPrice);
    }

    [Fact]
    public void Archived_product_cannot_be_ordered_or_restocked()
    {
        var producer = TestData.ApprovedProducer();
        var product = TestData.ProductWithStock(producer);
        var slot = TestData.Slot(producer);
        product.Archive();

        Assert.Null(product.GetNextAvailableStock(TestData.Now));
        Assert.Throws<DomainException>(() => Order.Place(TestData.Customer(), slot, [(product, 1m)], TestData.Now));
        Assert.Throws<DomainException>(() => product.UpdateWeeklyStock(TestData.Saturday, 5m, Units.Kilogram, 500m));
    }
}

public class StockReviewHandlerTests
{
    [Fact]
    public async Task Closed_week_shows_leftover_and_a_hint()
    {
        var producer = TestData.ApprovedProducer().WithId(1);
        var plenty = TestData.ProductWithStock(producer, quantity: 10m).WithId(1);  // 2 kg rendelve → 80% maradt
        var slot = TestData.Slot(producer, capacity: 5);
        Order.Place(TestData.Customer(), slot, [(plenty, 2m)], TestData.Now);
        var soldOut = Product.Create(producer, "Körte", "", [Category.Create("Gyümölcs", "")]).WithId(2);
        soldOut.AddWeeklyStock(TestData.Saturday, 1m, Units.Kilogram, 600m);
        Order.Place(TestData.Customer(), slot, [(soldOut, 1m)], TestData.Now);

        var handler = new GetMyStockReviewHandler(new FakeProductRepository(plenty, soldOut),
            new FakeCurrentUser(9, Role.Producer, producerId: 1), new FixedTimeProvider(TestData.AfterPickup));
        var lines = await handler.HandleAsync();

        var a = lines.Single(l => l.ProductId == 1);
        Assert.Equal(nameof(StockVerdict.HighLeftover), a.Verdict);
        Assert.Equal(8m, a.Leftover);
        Assert.Equal(nameof(StockVerdict.SoldOut), lines.Single(l => l.ProductId == 2).Verdict);
    }

    [Fact]
    public async Task Open_week_is_not_reviewed_yet()
    {
        var producer = TestData.ApprovedProducer().WithId(1);
        var product = TestData.ProductWithStock(producer).WithId(1);
        var handler = new GetMyStockReviewHandler(new FakeProductRepository(product),
            new FakeCurrentUser(9, Role.Producer, producerId: 1), new FixedTimeProvider(TestData.OnPickupDay));
        Assert.Empty(await handler.HandleAsync());
    }
}

public class DeleteProductHandlerTests
{
    private static (Order Order, Product Product, PickupSlot Slot) ActiveOrder()
    {
        var producer = TestData.ApprovedProducer().WithId(1);
        var product = TestData.ProductWithStock(producer).WithId(7);
        var slot = TestData.Slot(producer).WithId(3);
        var order = Order.Place(TestData.Customer().WithId(5), slot, [(product, 2m)], TestData.Now).WithId(100);
        order.SetPayment(PaymentMethod.Cash);
        order.Accept(TestData.Now);
        return (order, product, slot);
    }

    private static DeleteProductHandler Handler(FakeOrderRepository orders, FakeProductRepository products, FakeCurrentUser user)
        => new(products, orders, new FakeCartRepository(), new FakeAuditLog(), new FakeImageStorage(), new FakeUnitOfWork(),
               user, new FixedTimeProvider(TestData.Now));

    [Fact]
    public async Task Manager_cancels_active_orders_and_archives_the_product()
    {
        var (order, product, slot) = ActiveOrder();
        var products = new FakeProductRepository(product) { HasOrderHistory = true };
        var handler = Handler(new FakeOrderRepository(order), products, new FakeCurrentUser(1, Role.Admin));

        var result = await handler.HandleAsync(new DeleteProductCommand(7, "Élelmiszer-biztonsági ok"));

        Assert.True(result.Archived);
        Assert.Equal(new[] { 100 }, result.CancelledOrderIds);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Contains("Élelmiszer-biztonsági ok", order.RejectionReason);
        Assert.True(product.IsArchived);
        Assert.Equal(0m, product.GetStockForWeek(TestData.Saturday)!.ReservedQuantity);
        Assert.Equal(0, slot.BookedCount);
    }

    [Fact]
    public async Task Manager_needs_a_reason_when_orders_would_be_cancelled()
    {
        var (order, product, _) = ActiveOrder();
        var handler = Handler(new FakeOrderRepository(order), new FakeProductRepository(product), new FakeCurrentUser(1, Role.Admin));
        await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(new DeleteProductCommand(7)));
        Assert.Equal(OrderStatus.Accepted, order.Status);
    }

    [Fact]
    public async Task Producer_cannot_delete_a_product_with_active_orders()
    {
        var (order, product, _) = ActiveOrder();
        var handler = Handler(new FakeOrderRepository(order), new FakeProductRepository(product), new FakeCurrentUser(9, Role.Producer, producerId: 1));
        await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(new DeleteProductCommand(7)));
    }

    [Fact]
    public async Task Product_without_orders_is_physically_deleted()
    {
        var producer = TestData.ApprovedProducer().WithId(1);
        var product = TestData.ProductWithStock(producer).WithId(7);
        var products = new FakeProductRepository(product);
        var handler = Handler(new FakeOrderRepository(), products, new FakeCurrentUser(9, Role.Producer, producerId: 1));

        var result = await handler.HandleAsync(new DeleteProductCommand(7));

        Assert.False(result.Archived);
        Assert.Contains(product, products.Removed);
    }
}
