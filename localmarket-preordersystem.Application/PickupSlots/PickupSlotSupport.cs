using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Interfaces;

namespace localmarket_preordersystem.Application.PickupSlots
{
    internal static class PickupSlotSupport
    {
        /// <summary>
        /// Egy árusnak egy napon nem lehet két, egymást átfedő idősávja (pl. 8–10 és 9–11).
        /// Az érintkező sávok (8–10 és 10–12) megengedettek. Módosításnál a saját sávot ki kell hagyni.
        /// </summary>
        public static async Task EnsureNoOverlapAsync(
            IPickupSlotRepository slots, int producerId, DateTime date, TimeOnly startTime, TimeOnly endTime,
            int? excludeSlotId, CancellationToken ct)
        {
            var sameDayOrLater = await slots.ListByProducerAsync(producerId, date.Date, ct);
            var clash = sameDayOrLater.FirstOrDefault(s => s.Id != excludeSlotId && s.Overlaps(date, startTime, endTime));
            if (clash is not null)
                throw new ConflictException(
                    $"Ezen a napon már van átfedő átvételi időpontod ({clash.StartTime:HH\\:mm}–{clash.EndTime:HH\\:mm}).");
        }
    }
}
