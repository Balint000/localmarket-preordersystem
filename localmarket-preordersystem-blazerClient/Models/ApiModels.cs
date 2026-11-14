namespace localmarket_preordersystem.Models;

// Az API DTO-inak kliensoldali tükre (JSON, camelCase). Az enumok névvel utaznak (a szerver JsonStringEnumConverter-t használ).

public sealed record AuthResult(string Token, DateTime ExpiresAtUtc, int UserId, string Role, int? ProducerId);
public sealed record RegisteredResult(int Id);

public sealed record MarketInfo(int Id, string Name, string Address, bool IsActive, double? Latitude, double? Longitude, IReadOnlyList<string> OpeningDays);
public sealed record CategoryInfo(int Id, string Name, string Description);

public sealed record ProductStockInfo(DateTime WeekStartDate, decimal Quantity, decimal ReservedQuantity, decimal AvailableQuantity, string Unit, decimal UnitPrice);
public sealed record ProductInfo(
    int Id, int ProducerId, string? ProducerName, string Name, string Description,
    IReadOnlyList<CategoryInfo> Categories, IReadOnlyList<string> Allergies,
    ProductStockInfo? NextStock, IReadOnlyList<ProductStockInfo> WeeklyStocks);

public sealed record PickupSlotInfo(int Id, int ProducerId, DateTime Date, TimeOnly StartTime, TimeOnly EndTime, int Capacity, int BookedCount, int FreeCapacity);

public sealed record CartItemInfo(int ProductId, string ProductName, decimal Quantity, decimal UnitPriceSnapshot, decimal LineTotal);
public sealed record CartGroupInfo(int ProducerId, string? ProducerName, decimal Subtotal, IReadOnlyList<CartItemInfo> Items);
public sealed record CartInfo(IReadOnlyList<CartGroupInfo> Groups, decimal EstimatedTotal)
{
    public int ItemCount => Groups.Sum(g => g.Items.Count);
}

public sealed record OrderItemInfo(int ProductId, string ProductName, string Unit, decimal UnitPrice, decimal Quantity, decimal FulfilledQuantity, decimal Total, string Status);
public sealed record PaymentInfo(string Method, string Status, decimal Amount);
public sealed record OrderInfo(
    int Id, int CustomerId, string? CustomerName, int ProducerId, string? ProducerName,
    int PickupSlotId, DateTime PickupDate, TimeOnly PickupStart, TimeOnly PickupEnd,
    string Status, decimal TotalAmount, DateTime CreatedAt, string? DisputeNote, string? RejectionReason,
    PaymentInfo? Payment, IReadOnlyList<OrderItemInfo> Items);

// kérések
public sealed record LoginRequest(string Email, string Password);
public sealed record RegisterCustomerRequest(string FirstName, string LastName, string Email, string Password, string PhoneNumber);
public sealed record RegisterProducerRequest(string FirstName, string LastName, string Email, string Password, string PhoneNumber, string ProducerName, int MarketId, int StallNumber);
public sealed record AddToCartRequest(int ProductId, decimal Quantity);
public sealed record QuantityRequest(decimal Quantity);
public sealed record PickupChoiceRequest(int ProducerId, int PickupSlotId);
public sealed record PlaceOrderRequest(string PaymentMethod, IReadOnlyList<PickupChoiceRequest> Pickups);
public sealed record ReasonRequest(string Reason);
public sealed record NoteRequest(string Note);
public sealed record ResolveRequest(string Decision, string? Note);
public sealed record FulfilledLineRequest(int ProductId, decimal Quantity);
public sealed record PartialRequest(IReadOnlyList<FulfilledLineRequest> Lines);

// árus / admin / időjárás / riport DTO-k
public sealed record ProducerInfo(int Id, string Name, int MarketId, int StallNumber, string Status, string? RejectionReason, DateTime RegisteredAt);
public sealed record MarketWeatherInfo(int MarketId, string MarketName, DateTime Date, bool IsMarketOpen, bool ForecastAvailable, double? TemperatureMinC, double? TemperatureMaxC, double? PrecipitationMm, string? Description);
public sealed record CancellationRateInfo(int TotalOrders, int CancelledOrders, int RejectedOrders, decimal Rate);
public sealed record TopProductInfo(int ProductId, string ProductName, decimal TotalQuantity, decimal TotalRevenue, int OrderCount);
public sealed record VendorRevenueInfo(int ProducerId, string ProducerName, int OrderCount, decimal Revenue);
public sealed record UnclaimedOrderInfo(int OrderId, int ProducerId, string ProducerName, int CustomerId, string CustomerName, DateTime PickupDate, decimal TotalAmount, string Status);

// további kérések
public sealed record CreateProductRequest(string Name, string? Description, IReadOnlyList<int> CategoryIds, IReadOnlyList<string> Allergies);
public sealed record UpdateProductRequest(string Name, string? Description, IReadOnlyList<int> CategoryIds, IReadOnlyList<string> Allergies);
public sealed record StockRequest(DateTime WeekDate, decimal Quantity, string Unit, decimal UnitPrice);
public sealed record CreatePickupSlotRequest(DateTime Date, TimeOnly StartTime, TimeOnly EndTime, int Capacity);
public sealed record UpdateMyProducerRequest(string Name, int StallNumber);
public sealed record CreateMarketRequest(string Name, string Address, IReadOnlyList<string> OpeningDays, double? Latitude, double? Longitude);
public sealed record UpdateMarketRequest(string Name, string Address, IReadOnlyList<string> OpeningDays, double? Latitude, double? Longitude, bool IsActive);
public sealed record CreateCategoryRequest(string Name, string? Description);
