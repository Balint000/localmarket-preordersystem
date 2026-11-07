using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Reports.CancellationRate;
using localmarket_preordersystem.Application.Reports.TopProducts;
using localmarket_preordersystem.Application.Reports.UnclaimedOrders;
using localmarket_preordersystem.Application.Reports.VendorRevenue;
using localmarket_preordersystem.Domain.ValueObject;
using localmarket_preordersystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace localmarket_preordersystem.Infrastructure.Persistence.Repositories;

internal sealed class ReportQueries(MarketDbContext context, TimeProvider time) : IReportQueries
{
    private static DateTime Utc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);

    public async Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(
        DateTime from, DateTime to, int top, CancellationToken ct = default)
    {
        var f = Utc(from);
        var t = Utc(to);

        var rows = await context.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= f && o.CreatedAt <= t
                        && o.Status != OrderStatus.Cancelled
                        && o.Status != OrderStatus.Rejected)
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                TotalQuantity = g.Sum(i => i.FulfilledQuantity),
                TotalRevenue = g.Sum(i => i.UnitPrice * i.FulfilledQuantity),
                OrderCount = g.Select(i => i.OrderId).Distinct().Count()
            })
            .OrderByDescending(x => x.TotalQuantity)
            .ThenBy(x => x.ProductName)
            .Take(top)
            .ToListAsync(ct);

        return rows
            .Select(r => new TopProductDto(r.ProductId, r.ProductName, r.TotalQuantity, r.TotalRevenue, r.OrderCount))
            .ToList();
    }

    public async Task<CancellationRateDto> GetCancellationRateAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var f = Utc(from);
        var t = Utc(to);

        var counts = await context.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= f && o.CreatedAt <= t)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Cancelled = g.Count(o => o.Status == OrderStatus.Cancelled),
                Rejected = g.Count(o => o.Status == OrderStatus.Rejected)
            })
            .FirstOrDefaultAsync(ct);

        if (counts is null || counts.Total == 0)
            return new CancellationRateDto(0, 0, 0, 0m);

        var rate = Math.Round((decimal)counts.Cancelled / counts.Total, 4);
        return new CancellationRateDto(counts.Total, counts.Cancelled, counts.Rejected, rate);
    }

    public async Task<IReadOnlyList<VendorRevenueDto>> GetVendorRevenueAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var f = Utc(from);
        var t = Utc(to);

        var orders = await context.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= f && o.CreatedAt <= t && o.Status == OrderStatus.PickedUp)
            .Select(o => new
            {
                o.ProducerId,
                Total = o.Items.Sum(i => i.UnitPrice * i.FulfilledQuantity)
            })
            .ToListAsync(ct);

        if (orders.Count == 0)
            return [];

        var ids = orders.Select(o => o.ProducerId).Distinct().ToList();
        var names = await context.Producers.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        return orders
            .GroupBy(o => o.ProducerId)
            .Select(g => new VendorRevenueDto(
                g.Key,
                names.GetValueOrDefault(g.Key, string.Empty),
                g.Count(),
                g.Sum(o => o.Total)))
            .OrderByDescending(r => r.Revenue)
            .ToList();
    }

    public async Task<IReadOnlyList<UnclaimedOrderDto>> GetUnclaimedOrdersAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var f = Utc(from);
        var t = Utc(to);
        var now = time.GetLocalNow().UtcDateTime;
        var today = now.Date;
        var nowTime = TimeOnly.FromDateTime(now);

        var rows = await context.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= f && o.CreatedAt <= t
                        && (o.Status == OrderStatus.NotPickedUp
                            || ((o.Status == OrderStatus.Accepted
                                 || o.Status == OrderStatus.Ready
                                 || o.Status == OrderStatus.PartiallyFulfilled)
                                && (o.PickupSlot.Date < today
                                    || (o.PickupSlot.Date == today && o.PickupSlot.EndTime <= nowTime)))))
            .OrderByDescending(o => o.PickupSlot.Date)
            .Select(o => new
            {
                OrderId = o.Id,
                o.ProducerId,
                CustomerId = o.UserId,
                CustomerFirst = o.User.FirstName,
                CustomerLast = o.User.LastName,
                PickupDate = o.PickupSlot.Date,
                Total = o.Items.Sum(i => i.UnitPrice * i.FulfilledQuantity),
                o.Status
            })
            .ToListAsync(ct);

        if (rows.Count == 0)
            return [];

        var ids = rows.Select(r => r.ProducerId).Distinct().ToList();
        var names = await context.Producers.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        return rows
            .Select(r => new UnclaimedOrderDto(
                r.OrderId,
                r.ProducerId,
                names.GetValueOrDefault(r.ProducerId, string.Empty),
                r.CustomerId,
                $"{r.CustomerLast} {r.CustomerFirst}".Trim(),
                r.PickupDate,
                r.Total,
                r.Status.ToString()))
            .ToList();
    }
}
