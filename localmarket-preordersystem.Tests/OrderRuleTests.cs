using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Export.ExportPickingList;
using localmarket_preordersystem.Application.Orders;
using localmarket_preordersystem.Application.Orders.CancelOrder;
using localmarket_preordersystem.Application.Orders.ExpirePendingOrders;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.Exceptions;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Tests;

/// <summary>Időkorlátok, lejárat, vita csak átadás után, foglalás a piac állapota szerint.</summary>
public class OrderTimeRuleTests
{
    // az idősáv szombat 9–12; a vevői lemondási határidő elfogadott rendelésnél péntek 21:00
    private static readonly DateTime FridayEvening = new(2026, 11, 20, 20, 0, 0);
    private static readonly DateTime FridayLate = new(2026, 11, 20, 22, 0, 0);
    private static readonly DateTime SaturdayAfterStart = new(2026, 11, 21, 9, 30, 0);

    [Fact]
    public void Dispute_is_not_possible_before_the_handover()
    {
        var (order, _, _) = TestData.PendingOrder();
        order.Accept(TestData.Now);
        Assert.Throws<DomainException>(() => order.Dispute("Korai panasz", TestData.OnPickupDay));
        order.MarkReady(TestData.Now);
        Assert.Throws<DomainException>(() => order.Dispute("Korai panasz", TestData.OnPickupDay));
        Assert.True(order.HoldsReservations);
    }

    [Fact]
    public void Unclosed_handover_can_be_disputed_after_the_slot_ended()
    {
        // az árus nem jelölte az átadást: a vevő az idősáv után vitát nyithat (különben a rendelés beragadna)
        var (order, _, _) = TestData.PendingOrder();
        order.Accept(TestData.Now);
        order.MarkReady(TestData.Now);

        order.Dispute("Nem adták ki a rendelést", TestData.AfterPickup);

        Assert.Equal(OrderStatus.Disputed, order.Status);
        Assert.False(order.HoldsReservations);
    }

    [Fact]
    public void Customer_can_cancel_an_accepted_order_only_until_the_cutoff()
    {
        var (order, product, slot) = TestData.PendingOrder(2m);
        order.Accept(TestData.Now);

        Assert.Throws<DomainException>(() => order.CancelByCustomer(FridayLate, TestData.Cutoff));
        Assert.Equal(OrderStatus.Accepted, order.Status);

        var held = order.CancelByCustomer(FridayEvening, TestData.Cutoff);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(2m, Assert.Single(held).Quantity);
        Assert.NotNull(product);
        Assert.NotNull(slot);
    }

    [Fact]
    public void Customer_can_cancel_a_pending_order_until_the_slot_starts()
    {
        var (late, _, _) = TestData.PendingOrder();
        late.CancelByCustomer(FridayLate, TestData.Cutoff); // még nem fogadták el: a határidő nem vonatkozik rá
        Assert.Equal(OrderStatus.Cancelled, late.Status);

        var (started, _, _) = TestData.PendingOrder();
        Assert.Throws<DomainException>(() => started.CancelByCustomer(SaturdayAfterStart, TestData.Cutoff));
    }

    [Fact]
    public void Seller_cancellation_requires_a_reason_and_is_shown_to_the_customer()
    {
        var (order, _, _) = TestData.PendingOrder(2m);
        order.Accept(TestData.Now);

        Assert.Throws<DomainException>(() => order.CancelBySeller(TestData.Now, " "));
        var held = order.CancelBySeller(SaturdayAfterStart, "Elfagyott a termés");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Elfagyott a termés", order.RejectionReason);
        Assert.Equal(PaymentStatus.Cancelled, order.Payment!.Status);
        Assert.Single(held);
    }

    [Fact]
    public void Seller_cannot_cancel_after_the_slot_ended()
    {
        var (order, _, _) = TestData.PendingOrder();
        order.Accept(TestData.Now);
        Assert.Throws<DomainException>(() => order.CancelBySeller(TestData.AfterPickup, "Késő"));
    }

    [Fact]
    public void Picked_up_only_on_the_pickup_day()
    {
        var (order, _, _) = TestData.PendingOrder();
        order.Accept(TestData.Now);
        Assert.Throws<DomainException>(() => order.MarkPickedUp(TestData.Now));
        order.MarkPickedUp(TestData.OnPickupDay);
        Assert.Equal(OrderStatus.PickedUp, order.Status);
    }

    [Fact]
    public void Not_picked_up_only_after_the_slot_ended()
    {
        var (order, _, _) = TestData.PendingOrder();
        order.Accept(TestData.Now);
        Assert.Throws<DomainException>(() => order.MarkNotPickedUp(TestData.OnPickupDay));
        order.MarkNotPickedUp(TestData.AfterPickup);
        Assert.Equal(OrderStatus.NotPickedUp, order.Status);
    }

    [Fact]
    public void Accepting_or_marking_ready_after_the_slot_ended_is_rejected()
    {
        var (order, _, _) = TestData.PendingOrder();
        Assert.Throws<DomainException>(() => order.Accept(TestData.AfterPickup));
        order.Accept(TestData.Now);
        Assert.Throws<DomainException>(() => order.MarkReady(TestData.AfterPickup));
    }

    [Fact]
    public void Full_acceptance_marks_every_item_fulfilled()
    {
        var (order, _, _) = TestData.PendingOrder();
        order.Accept(TestData.Now);
        Assert.All(order.Items, i => Assert.Equal(OrderItemStatus.Fulfilled, i.Status));
    }

    [Fact]
    public void Pending_order_expires_only_after_the_slot_ended()
    {
        var (order, _, _) = TestData.PendingOrder(2m);
        Assert.Throws<DomainException>(() => order.Expire(TestData.OnPickupDay));

        var held = order.Expire(TestData.AfterPickup);

        Assert.Equal(OrderStatus.Expired, order.Status);
        Assert.Equal(PaymentStatus.Cancelled, order.Payment!.Status);
        Assert.Equal(2m, Assert.Single(held).Quantity);
        Assert.False(order.HoldsReservations);
    }

    [Fact]
    public void Only_pending_orders_expire()
    {
        var (order, _, _) = TestData.PendingOrder();
        order.Accept(TestData.Now);
        Assert.Throws<DomainException>(() => order.Expire(TestData.AfterPickup));
    }
}

public class SlotBookabilityTests
{
    [Fact]
    public void Slot_cannot_be_booked_when_the_market_is_inactive()
    {
        var producer = TestData.ApprovedProducer();
        var slot = TestData.Slot(producer);
        producer.Market.Deactivate();

        Assert.False(slot.IsBookable(TestData.Now));
        Assert.Throws<DomainException>(() => slot.Book(TestData.Now));
    }

    [Fact]
    public void Slot_cannot_be_booked_after_its_day_was_removed_from_the_opening_days()
    {
        var producer = TestData.ApprovedProducer();
        var slot = TestData.Slot(producer);
        producer.Market.SetOpeningDays([DayOfWeek.Friday]);

        Assert.False(slot.IsBookable(TestData.Now));
        var product = TestData.ProductWithStock(producer);
        Assert.Throws<DomainException>(() => Order.Place(TestData.Customer(), slot, [(product, 1m)], TestData.Now));
        Assert.Equal(0, slot.BookedCount);
    }

    [Fact]
    public void Open_slot_with_free_capacity_is_bookable()
        => Assert.True(TestData.Slot(TestData.ApprovedProducer()).IsBookable(TestData.Now));
}

public class CancelOrderHandlerTests
{
    private static (Order Order, Product Product, PickupSlot Slot) AcceptedOrder(int producerId, int customerId)
    {
        var producer = TestData.ApprovedProducer().WithId(producerId);
        var product = TestData.ProductWithStock(producer).WithId(7);
        var slot = TestData.Slot(producer).WithId(3);
        var customer = TestData.Customer().WithId(customerId);
        var order = Order.Place(customer, slot, [(product, 2m)], TestData.Now).WithId(100);
        order.SetPayment(PaymentMethod.Cash);
        order.Accept(TestData.Now);
        return (order, product, slot);
    }

    private static CancelOrderHandler Handler(Order order, Product product, FakeCurrentUser user, DateTime now)
        => new(new FakeOrderRepository(order), new FakeProductRepository(product), new FakeAuditLog(), new FakeUnitOfWork(),
               user, new FixedTimeProvider(now), OrderPolicy.Default);

    [Fact]
    public async Task Producer_cancels_own_order_with_reason_and_releases_stock_and_seat()
    {
        var (order, product, slot) = AcceptedOrder(producerId: 1, customerId: 5);
        var handler = Handler(order, product, new FakeCurrentUser(9, Role.Producer, producerId: 1), TestData.Now);

        var dto = await handler.HandleAsync(new CancelOrderCommand(100, "Elfagyott"));

        Assert.Equal("Cancelled", dto.Status);
        Assert.Equal(0m, product.GetStockForWeek(TestData.Saturday)!.ReservedQuantity);
        Assert.Equal(0, slot.BookedCount);
    }

    [Fact]
    public async Task Producer_needs_a_reason()
    {
        var (order, product, _) = AcceptedOrder(1, 5);
        var handler = Handler(order, product, new FakeCurrentUser(9, Role.Producer, producerId: 1), TestData.Now);
        await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(new CancelOrderCommand(100)));
    }

    [Fact]
    public async Task Producer_cannot_cancel_another_producers_order()
    {
        var (order, product, _) = AcceptedOrder(1, 5);
        var handler = Handler(order, product, new FakeCurrentUser(9, Role.Producer, producerId: 2), TestData.Now);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.HandleAsync(new CancelOrderCommand(100, "x")));
    }

    [Fact]
    public async Task Customer_cannot_cancel_someone_elses_order()
    {
        var (order, product, _) = AcceptedOrder(1, 5);
        var handler = Handler(order, product, new FakeCurrentUser(6, Role.Customer), TestData.Now);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.HandleAsync(new CancelOrderCommand(100)));
    }

    [Fact]
    public async Task Manager_can_cancel_any_order_with_reason()
    {
        var (order, product, _) = AcceptedOrder(1, 5);
        var handler = Handler(order, product, new FakeCurrentUser(1, Role.Admin), TestData.Now);
        var dto = await handler.HandleAsync(new CancelOrderCommand(100, "Elmarad a piacnap"));
        Assert.Equal("Cancelled", dto.Status);
        Assert.Equal("Elmarad a piacnap", dto.RejectionReason);
    }
}

public class ExpirePendingOrdersHandlerTests
{
    [Fact]
    public async Task Stale_pending_orders_expire_and_release_their_reservations()
    {
        var (stale, product, slot) = TestData.PendingOrder(2m);
        var audit = new FakeAuditLog();
        var uow = new FakeUnitOfWork();
        var handler = new ExpirePendingOrdersHandler(new FakeOrderRepository(stale), new FakeProductRepository(product), audit, uow,
            new FakeCurrentUser(null, null), new FixedTimeProvider(TestData.AfterPickup));

        var count = await handler.HandleAsync();

        Assert.Equal(1, count);
        Assert.Equal(OrderStatus.Expired, stale.Status);
        Assert.Equal(0m, product.GetStockForWeek(TestData.Saturday)!.ReservedQuantity);
        Assert.Equal(0, slot.BookedCount);
        Assert.Equal("OrderExpired", Assert.Single(audit.Entries).Action);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Nothing_happens_before_the_slot_ends()
    {
        var (order, product, _) = TestData.PendingOrder();
        var uow = new FakeUnitOfWork();
        var handler = new ExpirePendingOrdersHandler(new FakeOrderRepository(order), new FakeProductRepository(product), new FakeAuditLog(), uow,
            new FakeCurrentUser(null, null), new FixedTimeProvider(TestData.OnPickupDay));

        Assert.Equal(0, await handler.HandleAsync());
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(0, uow.SaveCount);
    }
}

public class PickingListHandlerTests
{
    [Fact]
    public async Task Manager_can_export_the_whole_market_day_and_pending_orders_are_left_out()
    {
        var (accepted, _, _) = TestData.PendingOrder();
        accepted.Accept(TestData.Now);
        var (pending, _, _) = TestData.PendingOrder();
        var handler = new ExportPickingListHandler(new FakeOrderRepository(accepted.WithId(1), pending.WithId(2)), new FakeCurrentUser(1, Role.Admin));

        var file = await handler.HandleAsync(new ExportPickingListQuery(TestData.Saturday, ExportFormat.Json));
        var json = System.Text.Encoding.UTF8.GetString(file.Content);

        Assert.Contains("\"OrderId\": 1", json);
        Assert.DoesNotContain("\"OrderId\": 2", json);
        Assert.Contains("\"PendingOrders\": 1", json);
    }

    [Fact]
    public async Task Producer_cannot_request_another_producers_list()
    {
        var handler = new ExportPickingListHandler(new FakeOrderRepository(), new FakeCurrentUser(9, Role.Producer, producerId: 1));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.HandleAsync(new ExportPickingListQuery(TestData.Saturday, ExportFormat.Csv, ProducerId: 2)));
    }
}
