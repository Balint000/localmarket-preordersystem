using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.PickupSlots
{
    public sealed record PickupSlotDto(int Id, int ProducerId, DateTime Date, TimeOnly StartTime, TimeOnly EndTime, int Capacity, int BookedCount, int FreeCapacity)
    {
        public static PickupSlotDto From(PickupSlot s) => new(s.Id, s.ProducerId, s.Date, s.StartTime, s.EndTime, s.Capacity, s.BookedCount, s.Capacity - s.BookedCount);
    }
}
