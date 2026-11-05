using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Reports.CancellationRate;
using localmarket_preordersystem.Application.Reports.TopProducts;
using localmarket_preordersystem.Application.Reports.UnclaimedOrders;
using localmarket_preordersystem.Application.Reports.VendorRevenue;
using localmarket_preordersystem.Domain.ValueObject;
using localmarket_preordersystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace localmarket_preordersystem.Infrastructure.Persistence.Repositories;

internal sealed class ReportQueries(MarketDbContext context) : IReportQueries
{
    public async Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(DateTime from, DateTime to, int top, CancellationToken ct = default)
    {
        // TODO: igazítsd a TopProductDto konstruktorához
        var rows = await context.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to
                        && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Rejected)
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductId, i.Product.Name })
            .Select(g => new { g.Key.ProductId, g.Key.Name, Qty = g.Sum(i => i.Quantity) })
            .OrderByDescending(x => x.Qty)
            .Take(top)
            .ToListAsync(ct);

        return rows.Select(r => new TopProductDto(r.ProductId, r.Name, r.Qty)).ToList();
    }

    public async Task<CancellationRateDto> GetCancellationRateAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var orders = context.Orders.AsNoTracking().Where(o => o.CreatedAt >= from && o.CreatedAt <= to);
        var total = await orders.CountAsync(ct);
        var cancelled = await orders.CountAsync(o => o.Status == OrderStatus.Cancelled, ct);
        var rate = total == 0 ? 0m : Math.Round((decimal)cancelled / total * 100m, 2);

        // TODO: igazítsd a CancellationRateDto konstruktorához
        return new CancellationRateDto(total, cancelled, rate);
    }

    public async Task<IReadOnlyList<VendorRevenueDto>> GetVendorRevenueAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rows = await context.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to && o.Status == OrderStatus.PickedUp)
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.Product.ProducerId, i.Product.Producer.Name })
            .Select(g => new { g.Key.ProducerId, g.Key.Name, Revenue = g.Sum(i => i.UnitPrice * i.Quantity) })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync(ct);

        return rows.Select(r => new VendorRevenueDto(r.ProducerId, r.Name, r.Revenue)).ToList();
    }

    public async Task<IReadOnlyList<UnclaimedOrderDto>> GetUnclaimedOrdersAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rows = await context.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to && o.Status == OrderStatus.NotPickedUp)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new { o.Id, o.UserId, o.CreatedAt })
            .ToListAsync(ct);

        return rows.Select(r => new UnclaimedOrderDto(r.Id, r.UserId, r.CreatedAt)).ToList();
    }
}
