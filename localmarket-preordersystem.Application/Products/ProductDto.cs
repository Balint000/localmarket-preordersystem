using localmarket_preordersystem.Application.Categories;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Products
{
    /// <param name="OrderStep">A rendelhető lépték (kg/l: 0,5; dkg: 10; g/ml: 100; darabos: 1) — a kliens ehhez igazítja a léptetőt.</param>
    public sealed record ProductStockDto(DateTime WeekStartDate, decimal Quantity, decimal ReservedQuantity, decimal AvailableQuantity, string Unit, decimal UnitPrice, decimal OrderStep)
    {
        public static ProductStockDto From(ProductStock s) => new(s.WeekStartDate, s.Quantity, s.ReservedQuantity, s.AvailableQuantity, s.Unit.ToString(), s.UnitPrice, s.Unit.OrderStep());
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
        IReadOnlyList<ProductStockDto> WeeklyStocks,
        bool IsArchived = false)
    {
        /// <summary>A NextStock az aktuális (vagy legközelebbi jövőbeli) hét kínálata. Az ImageUrl az API-hoz képest relatív útvonal.</summary>
        /// <param name="now">Helyi idő: a NextStock az a legközelebbi hét, amelyre még rendelni lehet.</param>
        public static ProductDto From(Product p, DateTime now) => new(
            p.Id,
            p.ProducerId,
            p.Producer?.Name,
            p.Name,
            p.Description,
            ProductImages.UrlFor(p.ImageFileName),
            p.Categories.Select(CategoryDto.From).ToList(),
            p.Allergies.Select(a => a.ToString()).ToList(),
            p.GetNextAvailableStock(now) is { } s ? ProductStockDto.From(s) : null,
            p.WeeklyStocks.OrderBy(s => s.WeekStartDate).Select(ProductStockDto.From).ToList(),
            p.IsArchived);
    }
}
