using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

using System.Text.Json;
using localmarket_preordersystem.Application.Common.Util;

namespace localmarket_preordersystem.Application.Export.ExportPickingList
{
    public sealed record ExportPickingListQuery(DateTime PickupDate, ExportFormat Format);

    public sealed record PickingLine(string PickupTime, int OrderId, string Customer, string Product, string Unit, decimal Quantity);
    public sealed record PickingTotal(string Product, string Unit, decimal Quantity);
    public sealed record PickingListDto(DateTime PickupDate, IReadOnlyList<PickingLine> Lines, IReadOnlyList<PickingTotal> Totals);

    /// <summary>Az árus csomagolólistája egy átvételi napra: rendelésenként tételek (idősáv szerint) és termékenkénti összesítés.</summary>
    public sealed class ExportPickingListHandler(IOrderRepository orders, ICurrentUserService currentUser)
    {
        public async Task<ExportFile> HandleAsync(ExportPickingListQuery query, CancellationToken ct = default)
        {
            var producerId = currentUser.RequireProducerId();
            new ValidationBuilder().Must(nameof(query.Format), Enum.IsDefined(query.Format), "Érvénytelen formátum.").ThrowIfInvalid();

            var list = (await orders.ListForPickingAsync(producerId, query.PickupDate.Date, ct))
                .Where(o => o.Status is not (OrderStatus.Cancelled or OrderStatus.Rejected))
                .OrderBy(o => o.PickupSlot.StartTime).ThenBy(o => o.Id)
                .ToList();

            var lines = list.SelectMany(o => o.Items.Where(i => i.FulfilledQuantity > 0).Select(i => new PickingLine(
                $"{o.PickupSlot.StartTime:HH\\:mm}-{o.PickupSlot.EndTime:HH\\:mm}",
                o.Id,
                o.User is null ? $"Vevő #{o.UserId}" : $"{o.User.LastName} {o.User.FirstName}".Trim(),
                i.ProductName, i.Unit.ToString(), i.FulfilledQuantity))).ToList();

            var totals = lines.GroupBy(l => (l.Product, l.Unit))
                .Select(g => new PickingTotal(g.Key.Product, g.Key.Unit, g.Sum(l => l.Quantity)))
                .OrderBy(t => t.Product).ToList();

            var dto = new PickingListDto(query.PickupDate.Date, lines, totals);
            var stamp = query.PickupDate.ToString("yyyy-MM-dd");

            if (query.Format == ExportFormat.Json)
                return new ExportFile($"csomagololista-{stamp}.json", "application/json", JsonSerializer.SerializeToUtf8Bytes(dto, ExportSupport.Json));

            var csv = new CsvWriter().Row("Átvételi idő", "Rendelés", "Vevő", "Termék", "Egység", "Mennyiség");
            foreach (var l in lines) csv.Row(l.PickupTime, l.OrderId, l.Customer, l.Product, l.Unit, l.Quantity);
            csv.BlankLine().Row("Összesítés").Row("Termék", "Egység", "Mennyiség");
            foreach (var t in totals) csv.Row(t.Product, t.Unit, t.Quantity);
            return new ExportFile($"csomagololista-{stamp}.csv", "text/csv", csv.ToBytes());
        }
    }
}
