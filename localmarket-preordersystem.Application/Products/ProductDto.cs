using localmarket_preordersystem.Application.Categories;
using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Products
{
    public sealed record ProductStockDto(DateTime WeekStartDate, decimal Quantity, decimal ReservedQuantity, decimal AvailableQuantity, string Unit, decimal UnitPrice)
    {
        public static ProductStockDto From(ProductStock s) => new(s.WeekStartDate, s.Quantity, s.ReservedQuantity, s.AvailableQuantity, s.Unit.ToString(), s.UnitPrice);
    }

    public sealed record ProductDto(
        int Id,
        int ProducerId,
        string? ProducerName,
        string Name,
        string Description,
        string? ImageUrl,
        IReadOnlyList<CategoryDto> Categories,
        IReadOnlyList<string> Allergies,
        ProductStockDto? NextStock,
        IReadOnlyList<ProductStockDto> WeeklyStocks)
    {
        /// <summary>A NextStock az aktuális (vagy legközelebbi jövőbeli) hét kínálata. Az ImageUrl az API-hoz képest relatív útvonal.</summary>
        public static ProductDto From(Product p, DateTime today) => new(
            p.Id,
            p.ProducerId,
            p.Producer?.Name,
            p.Name,
            p.Description,
            ProductImages.UrlFor(p.ImageFileName),
            p.Categories.Select(CategoryDto.From).ToList(),
            p.Allergies.Select(a => a.ToString()).ToList(),
            p.GetNextAvailableStock(today) is { } s ? ProductStockDto.From(s) : null,
            p.WeeklyStocks.OrderBy(s => s.WeekStartDate).Select(ProductStockDto.From).ToList());
    }
}
