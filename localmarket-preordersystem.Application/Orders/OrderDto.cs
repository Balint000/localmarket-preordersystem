using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Orders
{
    public sealed record OrderItemDto(int ProductId, string ProductName, string Unit, decimal UnitPrice, decimal Quantity, decimal FulfilledQuantity, decimal Total, string Status);

    public sealed record PaymentDto(string Method, string Status, decimal Amount);

    public sealed record OrderDto(
        int Id,
        int CustomerId,
        string? CustomerName,
        int ProducerId,
        string? ProducerName,
        int PickupSlotId,
        DateTime PickupDate,
        TimeOnly PickupStart,
        TimeOnly PickupEnd,
        string Status,
        decimal TotalAmount,
        DateTime CreatedAt,
        string? DisputeNote,
        string? RejectionReason,
        PaymentDto? Payment,
        IReadOnlyList<OrderItemDto> Items)
    {
        public static OrderDto From(Order o) => new(
            o.Id,
            o.UserId,
            o.User is null ? null : $"{o.User.LastName} {o.User.FirstName}".Trim(),
            o.ProducerId,
            o.PickupSlot?.Producer?.Name,
            o.PickupSlotId,
            o.PickupSlot?.Date ?? default,
            o.PickupSlot?.StartTime ?? default,
            o.PickupSlot?.EndTime ?? default,
            o.Status.ToString(),
            o.TotalAmount,
            o.CreatedAt,
            o.DisputeNote,
            o.RejectionReason,
            o.Payment is null ? null : new PaymentDto(o.Payment.Method.ToString(), o.Payment.Status.ToString(), o.Payment.Amount),
            o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Unit.ToString(), i.UnitPrice, i.Quantity, i.FulfilledQuantity, i.Total, i.Status.ToString())).ToList());
    }
}
