using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Reports.VendorRevenue
{
    /// <summary>Árusonkénti bevétel: az átvett (PickedUp) rendelések teljesített értéke.</summary>
    public sealed record VendorRevenueDto(int ProducerId, string ProducerName, int OrderCount, decimal Revenue);
    public sealed record VendorRevenueQuery(DateTime? From = null, DateTime? To = null);

    public sealed class VendorRevenueHandler(IReportQueries reports, ICurrentUserService currentUser, TimeProvider time)
    {
        public async Task<IReadOnlyList<VendorRevenueDto>> HandleAsync(VendorRevenueQuery query, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Admin);
            var (from, to) = ReportRange.Resolve(query.From, query.To, time);
            return await reports.GetVendorRevenueAsync(from, to, ct);
        }
    }
}
