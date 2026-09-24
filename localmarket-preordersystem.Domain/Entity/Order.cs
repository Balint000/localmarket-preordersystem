using localmarket_preordersystem.Domain.ValueObject;
using System;
using System.Collections.Generic;
using System.Text;

namespace localmarket_preordersystem.Domain.Entity
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Pending]            = [OrderStatus.Accepted, OrderStatus.PartiallyFulfilled, OrderStatus.Rejected, OrderStatus.Cancelled],
        [OrderStatus.Accepted]           = [OrderStatus.Ready, OrderStatus.PickedUp, OrderStatus.Cancelled, OrderStatus.NotPickedUp, OrderStatus.Disputed],
        [OrderStatus.PartiallyFulfilled] = [OrderStatus.Ready, OrderStatus.PickedUp, OrderStatus.Cancelled, OrderStatus.NotPickedUp, OrderStatus.Disputed],
        [OrderStatus.Ready]              = [OrderStatus.PickedUp, OrderStatus.Cancelled, OrderStatus.NotPickedUp, OrderStatus.Disputed],
        [OrderStatus.PickedUp]           = [OrderStatus.Disputed],
        [OrderStatus.NotPickedUp]        = [OrderStatus.Disputed],
        [OrderStatus.Disputed]           = [OrderStatus.PickedUp, OrderStatus.Cancelled],
        [OrderStatus.Cancelled]          = [],
        [OrderStatus.Rejected]           = [],
    };

    public class Order
    {
        public int Id { get; private set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        // public decimal TotalAmount { get; set; }
        public List<OrderItem> Items { get; set; } = new();

        public int PickupSlotId { get; set; }
        public PickupSlot PickupSlot { get; set; } = null!;
        public string? DisputeNote { get; set; } // felülvizsgálat komment
        public Payment? Payment { get; set; }

        public decimal TotalAmount => Items.Sum(i => i.Total);
    }
}
