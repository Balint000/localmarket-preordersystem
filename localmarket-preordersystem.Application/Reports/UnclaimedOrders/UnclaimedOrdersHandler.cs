using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Reports.UnclaimedOrders
{
    /// <summary>Át nem vett rendelés: NotPickedUp, vagy elfogadott/kész, de az átvételi idősáv már lejárt.</summary>
    public sealed record UnclaimedOrderDto(int OrderId, int ProducerId, string ProducerName, int CustomerId, string CustomerName, DateTime PickupDate, decimal TotalAmount, string Status);
    public sealed record UnclaimedOrdersQuery(DateTime? From = null, DateTime? To = null);

    public sealed class UnclaimedOrdersHandler(IReportQueries reports, ICurrentUserService currentUser, TimeProvider time)
    {
        public async Task<IReadOnlyList<UnclaimedOrderDto>> HandleAsync(UnclaimedOrdersQuery query, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Admin);
            var (from, to) = ReportRange.Resolve(query.From, query.To, time);
            return await reports.GetUnclaimedOrdersAsync(from, to, ct);
        }
    }
}
