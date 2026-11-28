using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Cart
{
    /// <param name="Unit">A legközelebbi rendelhető hét mértékegysége (null, ha jelenleg nincs rendelhető kínálat).</param>
    /// <param name="OrderStep">A léptető lépésköze ehhez az egységhez.</param>
    public sealed record CartItemDto(int ProductId, string ProductName, decimal Quantity, decimal UnitPriceSnapshot, decimal LineTotal, string? Unit = null, decimal OrderStep = 1m);

    public sealed record CartProducerGroupDto(int ProducerId, string? ProducerName, decimal Subtotal, IReadOnlyList<CartItemDto> Items);

    /// <summary>Többárusos kosár: árusonként csoportosítva (a rendelés leadásakor árusonként külön Order lesz).
    /// Az árak tájékoztató pillanatképek, a végső ár a rendeléskor, az átvételi hét kínálatából dől el.</summary>
    public sealed record CartDto(IReadOnlyList<CartProducerGroupDto> Groups, decimal EstimatedTotal)
    {
        public static CartDto Empty { get; } = new(Array.Empty<CartProducerGroupDto>(), 0m);

        public static CartDto From(Domain.Entity.Cart cart, DateTime now)
        {
            var groups = cart.Items
                .GroupBy(i => i.Product.ProducerId)
                .Select(g =>
                {
                    var items = g.Select(i =>
                    {
                        var stock = i.Product.GetNextAvailableStock(now);
                        return new CartItemDto(i.ProductId, i.Product.Name, i.Quantity, i.UnitPriceSnapshot, i.Quantity * i.UnitPriceSnapshot,
                            stock?.Unit.ToString(), stock?.Unit.OrderStep() ?? 1m);
                    }).ToList();
                    return new CartProducerGroupDto(g.Key, g.First().Product.Producer?.Name, items.Sum(i => i.LineTotal), items);
                })
                .OrderBy(g => g.ProducerId)
                .ToList();
            return new CartDto(groups, groups.Sum(g => g.Subtotal));
        }
    }
}
