namespace localmarket_preordersystem.Models;

// Az API DTO-inak kliensoldali tükre (JSON, camelCase). Az enumok névvel utaznak (a szerver JsonStringEnumConverter-t használ).

/// <summary>A lapozott végpontok burkoló válasza (api: PagedResult).</summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize, int TotalPages);

public sealed record AuthResult(string Token, DateTime ExpiresAtUtc, int UserId, string Role, int? ProducerId);
public sealed record RegisteredResult(int Id);

public sealed record MarketInfo(int Id, string Name, string Address, bool IsActive, double? Latitude, double? Longitude, IReadOnlyList<string> OpeningDays,
    TimeOnly? OpensAt = null, TimeOnly? ClosesAt = null)
{
    /// <summary>Nyitvatartás szövegesen, pl. „07:00–13:00”.</summary>
    public string HoursText => OpensAt is { } o && ClosesAt is { } c ? $"{o:HH\\:mm}–{c:HH\\:mm}" : "nincs megadva";
}
public sealed record CategoryInfo(int Id, string Name, string Description);
public sealed record AuditLogInfo(int Id, int? UserId, string Action, string EntityName, int EntityId, string? OldValue, string? NewValue, DateTime CreatedAt);

public sealed record ProductStockInfo(DateTime WeekStartDate, decimal Quantity, decimal ReservedQuantity, decimal AvailableQuantity, string Unit, decimal UnitPrice, decimal OrderStep = 1m);
public sealed record ProductInfo(
    int Id, int ProducerId, string? ProducerName, string Name, string Description,
    IReadOnlyList<CategoryInfo> Categories, IReadOnlyList<string> Allergies,
    ProductStockInfo? NextStock, IReadOnlyList<ProductStockInfo> WeeklyStocks,
    string? ImageUrl = null, bool IsArchived = false);


public sealed record PickupSlotInfo(int Id, int ProducerId, DateTime Date, TimeOnly StartTime, TimeOnly EndTime, int Capacity, int BookedCount, int FreeCapacity, bool IsBookable = true);

public sealed record CartItemInfo(int ProductId, string ProductName, decimal Quantity, decimal UnitPriceSnapshot, decimal LineTotal, string? Unit = null, decimal OrderStep = 1m);
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
public sealed record CancelRequest(string? Reason);
public sealed record ResolveRequest(string Decision, string? Note);
public sealed record FulfilledLineRequest(int ProductId, decimal Quantity);
public sealed record PartialRequest(IReadOnlyList<FulfilledLineRequest> Lines);

// árus / admin / időjárás / riport DTO-k
public sealed record ProducerInfo(int Id, string Name, int MarketId, int StallNumber, string Status, string? RejectionReason, DateTime RegisteredAt);
public sealed record MarketWeatherInfo(int MarketId, string MarketName, DateTime Date, bool IsMarketOpen, bool ForecastAvailable, double? TemperatureMinC, double? TemperatureMaxC, double? PrecipitationMm, string? Description);
public sealed record CancellationRateInfo(int TotalOrders, int CancelledOrders, int RejectedOrders, decimal Rate, int ExpiredOrders = 0);
public sealed record TopProductInfo(int ProductId, string ProductName, decimal TotalQuantity, decimal TotalRevenue, int OrderCount);
public sealed record VendorRevenueInfo(int ProducerId, string ProducerName, int OrderCount, decimal Revenue);
public sealed record UnclaimedOrderInfo(int OrderId, int ProducerId, string ProducerName, int CustomerId, string CustomerName, DateTime PickupDate, decimal TotalAmount, string Status);

// további kérések
public sealed record CreateProductRequest(string Name, string? Description, IReadOnlyList<int> CategoryIds, IReadOnlyList<string> Allergies);
public sealed record UpdateProductRequest(string Name, string? Description, IReadOnlyList<int> CategoryIds, IReadOnlyList<string> Allergies);
public sealed record StockRequest(DateTime WeekDate, decimal Quantity, string Unit, decimal UnitPrice);
public sealed record CreatePickupSlotRequest(DateTime Date, TimeOnly StartTime, TimeOnly EndTime, int Capacity);
/// <summary>A standot a piacvezető osztja ki: a StallNumber elhagyható (null), módosítani nem lehet.</summary>
public sealed record UpdateMyProducerRequest(string Name, int? StallNumber = null);

// --- piac-elrendezés (felülnézeti térkép)
public sealed record StallInfo(int Number, int X, int Y, int Width, int Height, string? Label, int? ProducerId, string? ProducerName, string? ProducerStatus);
public sealed record UnassignedProducerInfo(int ProducerId, string ProducerName, int PreviousStallNumber, string Reason);
public sealed record MarketLayoutInfo(int MarketId, string MarketName, int Columns, int Rows, IReadOnlyList<StallInfo> Stalls, IReadOnlyList<UnassignedProducerInfo> UnassignedProducers);
public sealed record StallInputRequest(int Number, int X, int Y, int Width, int Height, string? Label);
public sealed record MarketLayoutRequest(int Columns, int Rows, IReadOnlyList<StallInputRequest> Stalls);
public sealed record AssignStallRequest(int StallNumber, bool Swap = false);
public sealed record CreateMarketRequest(string Name, string Address, IReadOnlyList<string> OpeningDays, double? Latitude, double? Longitude, TimeOnly? OpensAt = null, TimeOnly? ClosesAt = null);
public sealed record UpdateMarketRequest(string Name, string Address, IReadOnlyList<string> OpeningDays, double? Latitude, double? Longitude, bool IsActive, TimeOnly? OpensAt = null, TimeOnly? ClosesAt = null);

public sealed record DeleteProductResult(int ProductId, bool Archived, IReadOnlyList<int> CancelledOrderIds, IReadOnlyList<int> UntouchedOrderIds);
public sealed record StockReviewLine(int ProductId, string ProductName, DateTime WeekStartDate, string Unit,
    decimal Offered, decimal Ordered, decimal Leftover, decimal SellThrough, string Verdict, string Hint);
public sealed record CreateCategoryRequest(string Name, string? Description);
