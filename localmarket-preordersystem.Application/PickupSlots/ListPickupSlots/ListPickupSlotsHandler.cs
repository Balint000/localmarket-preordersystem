using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;
using localmarket_preordersystem.Application.Categories;

namespace localmarket_preordersystem.Application.PickupSlots.ListPickupSlots
{
    /// <summary>Egy árus még le nem járt idősávjai (vásárlóknak a rendeléshez). Nyilvános.</summary>
    public sealed class ListPickupSlotsHandler(IPickupSlotRepository slots, TimeProvider time)
    {
        public async Task<IReadOnlyList<PickupSlotDto>> HandleAsync(int producerId, CancellationToken ct = default)
        {
            var now = time.GetLocalNow().DateTime;
            var list = await slots.ListByProducerAsync(producerId, now.Date, ct);
            return list.Where(s => !s.HasEnded(now))
                       .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
                       .Select(PickupSlotDto.From).ToList();
        }
    }
}
