using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.PickupSlots
{
    /// <param name="IsBookable">
    /// Foglalható-e most: nem járt le, van szabad hely, az árus aktív, a piac működik és aznap nyitva tart.
    /// (Pl. ha a piacvezető kivette a napot a nyitvatartásból, a meglévő idősáv látszik, de nem foglalható.)
    /// </param>
    public sealed record PickupSlotDto(int Id, int ProducerId, DateTime Date, TimeOnly StartTime, TimeOnly EndTime, int Capacity, int BookedCount, int FreeCapacity, bool IsBookable)
    {
        /// <summary>A foglalhatósághoz a slot Producer.Market navigációjának betöltve kell lennie (különben false).</summary>
        public static PickupSlotDto From(PickupSlot s, DateTime now) =>
            new(s.Id, s.ProducerId, s.Date, s.StartTime, s.EndTime, s.Capacity, s.BookedCount, s.Capacity - s.BookedCount, s.IsBookable(now));
    }
}
