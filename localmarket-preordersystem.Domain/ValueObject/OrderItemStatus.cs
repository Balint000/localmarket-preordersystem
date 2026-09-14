namespace localmarket_preordersystem.Domain.ValueObject;

// egy darab item status
public enum OrderItemStatus
{
    Pending = 1,
    Fulfilled = 2,
    PartiallyFulfilled = 3,
    OutOfStock = 4
}
