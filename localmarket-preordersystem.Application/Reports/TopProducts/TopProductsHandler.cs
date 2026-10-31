using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Reports.TopProducts
{
    public sealed record TopProductDto(int ProductId, string ProductName, decimal TotalQuantity, decimal TotalRevenue, int OrderCount);
    public sealed record TopProductsQuery(DateTime? From = null, DateTime? To = null, int Top = 10);

    /// <summary>Legnépszerűbb termékek (teljesített mennyiség szerint). Piacvezetői riport.</summary>
    public sealed class TopProductsHandler(IReportQueries reports, ICurrentUserService currentUser, TimeProvider time)
    {
        public async Task<IReadOnlyList<TopProductDto>> HandleAsync(TopProductsQuery query, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Admin);
            new ValidationBuilder().Must(nameof(query.Top), query.Top is >= 1 and <= 100, "A Top értéke 1 és 100 között lehet.").ThrowIfInvalid();
            var (from, to) = ReportRange.Resolve(query.From, query.To, time);
            return await reports.GetTopProductsAsync(from, to, query.Top, ct);
        }
    }
}
