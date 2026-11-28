using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Producers.StockReview
{
    public enum StockVerdict { SoldOut = 1, HighLeftover = 2, NoOrders = 3, Balanced = 4 }

    /// <param name="Offered">A hétre meghirdetett mennyiség.</param>
    /// <param name="Ordered">Ténylegesen lefoglalt (rendelt, a lemondások és a részleges teljesítés hiánya nélkül).</param>
    /// <param name="Leftover">Ami a hét végére megmaradt (meghirdetett − rendelt).</param>
    /// <param name="SellThrough">Rendelt / meghirdetett (0–1).</param>
    public sealed record StockReviewLineDto(
        int ProductId, string ProductName, DateTime WeekStartDate, string Unit,
        decimal Offered, decimal Ordered, decimal Leftover, decimal SellThrough, string Verdict, string Hint);

    /// <summary>
    /// Visszajelzés az árusnak a lezárt hetekről (a hét utolsó piacnapja után): termékenként mennyit hirdetett meg,
    /// mennyit rendeltek, mennyi maradt — és javaslat a következő hétre (kevesebb is elég / több is elkelne).
    /// </summary>
    public sealed class GetMyStockReviewHandler(IProductRepository products, ICurrentUserService currentUser, TimeProvider time)
    {
        /// <summary>Ennél kisebb kihasználtságnál jelezzük, hogy legközelebb kevesebb is elég lehet.</summary>
        public const decimal HighLeftoverThreshold = 0.5m;

        public async Task<IReadOnlyList<StockReviewLineDto>> HandleAsync(int weeks = 4, CancellationToken ct = default)
        {
            var producerId = currentUser.RequireProducerId();
            new ValidationBuilder().Must(nameof(weeks), weeks is >= 1 and <= 12, "A hetek száma 1 és 12 között lehet.").ThrowIfInvalid();

            var now = time.GetLocalNow().DateTime;
            var oldestWeek = ProductStock.GetWeekStart(now).AddDays(-7 * weeks);
            var list = await products.SearchAsync(new ProductSearchCriteria(ProducerId: producerId), ct);

            return list
                .SelectMany(p => p.GetClosedWeekStocks(now)
                    .Where(s => s.WeekStartDate >= oldestWeek && s.Quantity > 0)
                    .Select(s => ToLine(p, s)))
                .OrderByDescending(l => l.WeekStartDate)
                .ThenBy(l => l.Verdict == nameof(StockVerdict.Balanced))
                .ThenBy(l => l.ProductName)
                .ToList();
        }

        private static StockReviewLineDto ToLine(Product p, ProductStock s)
        {
            var ordered = Math.Min(s.ReservedQuantity, s.Quantity);
            var leftover = s.Quantity - ordered;
            var rate = Math.Round(ordered / s.Quantity, 4);
            var (verdict, hint) = (ordered, leftover, rate) switch
            {
                (0, _, _) => (StockVerdict.NoOrders, "Erre a hétre nem érkezett rendelés: érdemes kevesebbet meghirdetni, vagy jobb képet/leírást adni."),
                (_, 0, _) => (StockVerdict.SoldOut, "Elfogyott: legközelebb több is elkelhet."),
                (_, _, < HighLeftoverThreshold) => (StockVerdict.HighLeftover, $"Sok maradt ({leftover:0.###}): legközelebb kevesebb is elég lehet."),
                _ => (StockVerdict.Balanced, $"Jól eltalált mennyiség, {leftover:0.###} maradt."),
            };
            return new StockReviewLineDto(p.Id, p.Name, s.WeekStartDate, s.Unit.ToString(), s.Quantity, ordered, leftover, rate, verdict.ToString(), hint);
        }
    }
}
