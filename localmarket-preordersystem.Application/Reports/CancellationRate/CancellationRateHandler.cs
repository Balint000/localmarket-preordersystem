using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Reports.CancellationRate
{
    /// <summary>Lemondási arány: Rate = lemondott / összes rendelés (0..1), az elutasítás külön számolt.</summary>
    public sealed record CancellationRateDto(int TotalOrders, int CancelledOrders, int RejectedOrders, decimal Rate);
    public sealed record CancellationRateQuery(DateTime? From = null, DateTime? To = null);

    public sealed class CancellationRateHandler(IReportQueries reports, ICurrentUserService currentUser, TimeProvider time)
    {
        public async Task<CancellationRateDto> HandleAsync(CancellationRateQuery query, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Admin);
            var (from, to) = ReportRange.Resolve(query.From, query.To, time);
            return await reports.GetCancellationRateAsync(from, to, ct);
        }
    }
}
