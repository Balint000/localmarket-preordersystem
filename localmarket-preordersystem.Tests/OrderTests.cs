using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.Exceptions;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Tests;

public class OrderTests
{
    [Fact]
    public void Place_reserves_stock_and_a_slot_seat()
    {
        var (order, product, slot) = TestData.PendingOrder(2m);

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(1, slot.BookedCount);
        Assert.Equal(2m, product.GetStockForWeek(TestData.Saturday)!.ReservedQuantity);
        Assert.Equal(1000m, order.TotalAmount);          // 2 kg x 500 Ft
        Assert.Equal(1000m, order.Payment!.Amount);
        Assert.True(order.HoldsReservations);
    }

    [Fact]
    public void Place_fails_without_items()
    {
        var producer = TestData.ApprovedProducer();
        var slot = TestData.Slot(producer);
        Assert.Throws<DomainException>(() => Order.Place(TestData.Customer(), slot, [], TestData.Now));
    }

    [Fact]
    public void Place_fails_when_stock_is_not_enough()
    {
        var producer = TestData.ApprovedProducer();
        var product = TestData.ProductWithStock(producer, quantity: 1m);
        var slot = TestData.Slot(producer);

        Assert.Throws<DomainException>(() => Order.Place(TestData.Customer(), slot, [(product, 5m)], TestData.Now));
    }

    [Fact]
    public void Place_fails_when_the_slot_is_full()
    {
        var producer = TestData.ApprovedProducer();
        var product = TestData.ProductWithStock(producer);
        var slot = TestData.Slot(producer, capacity: 1);

        Order.Place(TestData.Customer(), slot, [(product, 1m)], TestData.Now);
        Assert.Throws<DomainException>(() => Order.Place(TestData.Customer(), slot, [(product, 1m)], TestData.Now));
    }

    [Fact]
    public void Cancel_returns_the_held_reservations()
    {
        var (order, product, _) = TestData.PendingOrder(2m);

        var held = order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(PaymentStatus.Cancelled, order.Payment!.Status);
        var item = Assert.Single(held);
        Assert.Equal(2m, item.Quantity);
        Assert.False(order.HoldsReservations);
        Assert.NotNull(product);
    }

    [Fact]
    public void Reject_requires_a_reason()
    {
        var (order, _, _) = TestData.PendingOrder();
        Assert.Throws<DomainException>(() => order.Reject("  "));
    }

    [Fact]
    public void Reject_releases_and_cancels_the_payment()
    {
        var (order, _, _) = TestData.PendingOrder(2m);

        var held = order.Reject("Elfogyott");

        Assert.Equal(OrderStatus.Rejected, order.Status);
        Assert.Equal("Elfogyott", order.RejectionReason);
        Assert.Equal(2m, Assert.Single(held).Quantity);
        Assert.Equal(PaymentStatus.Cancelled, order.Payment!.Status);
    }

    [Fact]
    public void AcceptPartially_releases_the_shortfall_and_updates_the_amount()
    {
        var (order, product, _) = TestData.PendingOrder(4m);

        var released = order.AcceptPartially(new Dictionary<int, decimal> { [product.Id] = 1m });

        Assert.Equal(OrderStatus.PartiallyFulfilled, order.Status);
        Assert.Equal(3m, Assert.Single(released).Quantity);
        Assert.Equal(500m, order.TotalAmount);
        Assert.Equal(500m, order.Payment!.Amount);
    }

    [Fact]
    public void AcceptPartially_with_nothing_fulfilled_is_rejected()
    {
        var (order, product, _) = TestData.PendingOrder(2m);
        Assert.Throws<DomainException>(() => order.AcceptPartially(new Dictionary<int, decimal> { [product.Id] = 0m }));
    }

    [Fact]
    public void Happy_path_completes_the_payment_on_pickup()
    {
        var (order, _, _) = TestData.PickedUpOrder();

        Assert.Equal(OrderStatus.PickedUp, order.Status);
        Assert.Equal(PaymentStatus.Completed, order.Payment!.Status);
        Assert.False(order.HoldsReservations);
    }

    [Fact]
    public void Invalid_transition_is_rejected()
    {
        var (order, _, _) = TestData.PendingOrder();
        Assert.Throws<DomainException>(() => order.MarkReady());      // Pending -> Ready nem engedett
        Assert.Throws<DomainException>(() => order.MarkPickedUp());   // Pending -> PickedUp nem engedett
    }

    [Fact]
    public void Terminal_states_cannot_change()
    {
        var (order, _, _) = TestData.PendingOrder();
        order.Cancel();
        Assert.Throws<DomainException>(() => order.Accept());
        Assert.Throws<DomainException>(() => order.Cancel());
    }

    [Fact]
    public void Dispute_requires_a_note()
    {
        var (order, _, _) = TestData.PickedUpOrder();
        Assert.Throws<DomainException>(() => order.Dispute(""));
    }

    // --- regresszió: a vitatott rendelést csak a piacvezető zárhatja le ---

    [Fact]
    public void Disputed_order_cannot_be_cancelled_by_the_normal_cancel_path()
    {
        var (order, _, _) = TestData.PickedUpOrder();
        order.Dispute("Nem kaptam meg");

        Assert.Throws<DomainException>(() => order.Cancel());
        Assert.Equal(OrderStatus.Disputed, order.Status);
    }

    [Fact]
    public void Disputed_order_cannot_be_marked_picked_up_by_the_normal_path()
    {
        var (order, _, _) = TestData.PickedUpOrder();
        order.Dispute("Nem kaptam meg");

        Assert.Throws<DomainException>(() => order.MarkPickedUp());
    }

    [Fact]
    public void Resolve_dispute_as_cancelled_refunds_a_completed_payment()
    {
        var (order, _, _) = TestData.PickedUpOrder();
        order.Dispute("Nem kaptam meg");

        order.ResolveDisputeAsCancelled("Jogos a panasz");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(PaymentStatus.Refunded, order.Payment!.Status);
        Assert.Contains("Jogos a panasz", order.DisputeNote);
    }

    [Fact]
    public void Resolve_dispute_as_picked_up_completes_a_pending_payment()
    {
        // NotPickedUp -> Disputed útvonal: a fizetés még függőben van
        var (order, _, _) = TestData.PendingOrder();
        order.Accept();
        order.MarkNotPickedUp();
        order.Dispute("Ott voltam, de nem adták ki");
        Assert.Equal(PaymentStatus.Pending, order.Payment!.Status);

        order.ResolveDisputeAsPickedUp("Átvettnek tekintjük");

        Assert.Equal(OrderStatus.PickedUp, order.Status);
        Assert.Equal(PaymentStatus.Completed, order.Payment.Status);
    }

    [Fact]
    public void Only_disputed_orders_can_be_resolved()
    {
        var (order, _, _) = TestData.PendingOrder();
        Assert.Throws<DomainException>(() => order.ResolveDisputeAsCancelled("x"));
        Assert.Throws<DomainException>(() => order.ResolveDisputeAsPickedUp("x"));
    }
}
