using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

using System.Text.Json;
using localmarket_preordersystem.Application.Common.Util;
using localmarket_preordersystem.Application.Orders;

namespace localmarket_preordersystem.Application.Export.ExportOrderSummary
{
    public sealed record ExportOrderSummaryQuery(int OrderId, ExportFormat Format);

    /// <summary>Egy rendelés összesítője JSON vagy CSV formában. Vevő (saját), érintett árus, piacvezető.</summary>
    public sealed class ExportOrderSummaryHandler(IOrderRepository orders, ICurrentUserService currentUser)
    {
        public async Task<ExportFile> HandleAsync(ExportOrderSummaryQuery query, CancellationToken ct = default)
        {
            new ValidationBuilder().Must(nameof(query.Format), Enum.IsDefined(query.Format), "Érvénytelen formátum.").ThrowIfInvalid();
            var order = await OrderSupport.LoadAsync(orders, query.OrderId, ct);
            OrderSupport.RequireCanView(order, currentUser);
            var dto = OrderDto.From(order);

            if (query.Format == ExportFormat.Json)
                return new ExportFile($"rendeles-{dto.Id}.json", "application/json", JsonSerializer.SerializeToUtf8Bytes(dto, ExportSupport.Json));

            var csv = new CsvWriter()
                .Row("Rendelés", "Állapot", "Árus", "Átvétel napja", "Átvétel kezdete", "Átvétel vége")
                .Row(dto.Id, dto.Status, dto.ProducerName ?? dto.ProducerId.ToString(), dto.PickupDate, dto.PickupStart, dto.PickupEnd)
                .BlankLine()
                .Row("Termék", "Egység", "Egységár", "Rendelt", "Teljesített", "Tétel összeg", "Tétel állapot");
            foreach (var i in dto.Items)
                csv.Row(i.ProductName, i.Unit, i.UnitPrice, i.Quantity, i.FulfilledQuantity, i.Total, i.Status);
            csv.BlankLine().Row("Végösszeg", dto.TotalAmount);
            return new ExportFile($"rendeles-{dto.Id}.csv", "text/csv", csv.ToBytes());
        }
    }
}
