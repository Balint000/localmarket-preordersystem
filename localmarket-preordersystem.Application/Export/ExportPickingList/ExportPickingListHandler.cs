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
    /// <param name="ProducerId">Csak piacvezetőnél: egy árus listája; null = a piacnap összes árusa. Árusnál mindig a saját.</param>
    public sealed record ExportPickingListQuery(DateTime PickupDate, ExportFormat Format, int? ProducerId = null);

    public sealed record PickingLine(string Producer, string PickupTime, int OrderId, string Customer, string Product, string Unit, decimal Quantity, string Status);
    public sealed record PickingTotal(string Producer, string Product, string Unit, decimal Quantity);
    /// <param name="PendingOrders">Még el nem fogadott (függő) rendelések száma: nincsenek a listán, előbb dönteni kell róluk.</param>
    public sealed record PickingListDto(DateTime PickupDate, IReadOnlyList<PickingLine> Lines, IReadOnlyList<PickingTotal> Totals, int PendingOrders);

    /// <summary>
    /// Csomagolólista egy átvételi napra: rendelésenként tételek (idősáv szerint) és termékenkénti összesítés.
    /// Csak a vállalt (elfogadott, részben elfogadott, átvehető, illetve már lezárt átadású) rendelések kerülnek rá;
    /// a függők számát külön jelzi. Az árus a sajátját, a piacvezető egy árusét vagy a teljes piacnapot kérheti le.
    /// </summary>
    public sealed class ExportPickingListHandler(IOrderRepository orders, ICurrentUserService currentUser)
    {
        public async Task<ExportFile> HandleAsync(ExportPickingListQuery query, CancellationToken ct = default)
        {
            int? producerId;
            if (currentUser.IsManager())
            {
                currentUser.RequireUserId();
                producerId = query.ProducerId;
            }
            else
            {
                producerId = currentUser.RequireProducerId();
                if (query.ProducerId is { } requested && requested != producerId)
                    throw new ForbiddenAccessException("Csak a saját átvételi listádat töltheted le.");
            }
            new ValidationBuilder().Must(nameof(query.Format), Enum.IsDefined(query.Format), "Érvénytelen formátum.").ThrowIfInvalid();

            var all = await orders.ListForPickingAsync(producerId, query.PickupDate.Date, ct);
            var pending = all.Count(o => o.Status == OrderStatus.Pending);
            var list = all
                .Where(o => o.Status is not (OrderStatus.Pending or OrderStatus.Cancelled or OrderStatus.Rejected or OrderStatus.Expired))
                .OrderBy(o => ProducerName(o)).ThenBy(o => o.PickupSlot.StartTime).ThenBy(o => o.Id)
                .ToList();

            var lines = list.SelectMany(o => o.Items.Where(i => i.FulfilledQuantity > 0).Select(i => new PickingLine(
                ProducerName(o),
                $"{o.PickupSlot.StartTime:HH\\:mm}-{o.PickupSlot.EndTime:HH\\:mm}",
                o.Id,
                o.User is null ? $"Vevő #{o.UserId}" : $"{o.User.LastName} {o.User.FirstName}".Trim(),
                i.ProductName, i.Unit.ToString(), i.FulfilledQuantity, o.Status.ToString()))).ToList();

            var totals = lines.GroupBy(l => (l.Producer, l.Product, l.Unit))
                .Select(g => new PickingTotal(g.Key.Producer, g.Key.Product, g.Key.Unit, g.Sum(l => l.Quantity)))
                .OrderBy(t => t.Producer).ThenBy(t => t.Product).ToList();

            var dto = new PickingListDto(query.PickupDate.Date, lines, totals, pending);
            var stamp = query.PickupDate.ToString("yyyy-MM-dd");
            var scope = producerId is null ? "piac" : $"arus{producerId}";

            if (query.Format == ExportFormat.Json)
                return new ExportFile($"csomagololista-{scope}-{stamp}.json", "application/json", JsonSerializer.SerializeToUtf8Bytes(dto, ExportSupport.Json));

            var csv = new CsvWriter().Row("Árus", "Átvételi idő", "Rendelés", "Vevő", "Termék", "Egység", "Mennyiség", "Állapot");
            foreach (var l in lines) csv.Row(l.Producer, l.PickupTime, l.OrderId, l.Customer, l.Product, l.Unit, l.Quantity, l.Status);
            csv.BlankLine().Row("Összesítés").Row("Árus", "Termék", "Egység", "Mennyiség");
            foreach (var t in totals) csv.Row(t.Producer, t.Product, t.Unit, t.Quantity);
            if (pending > 0)
                csv.BlankLine().Row("Figyelem", $"{pending} függő (még el nem fogadott) rendelés nincs a listán.");
            return new ExportFile($"csomagololista-{scope}-{stamp}.csv", "text/csv", csv.ToBytes());
        }

        private static string ProducerName(Order o) => o.PickupSlot?.Producer?.Name ?? $"Árus #{o.ProducerId}";
    }
}
