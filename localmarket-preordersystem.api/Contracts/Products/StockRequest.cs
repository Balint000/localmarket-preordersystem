using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.api.Contracts.Products;

public sealed record StockRequest(DateTime WeekDate, decimal Quantity, Units Unit, decimal UnitPrice);
