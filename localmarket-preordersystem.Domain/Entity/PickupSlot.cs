namespace localmarket_preordersystem.Domain.Entity
{
    public class PickupSlot
    {
        public int Id { get; set; }
        public int MarketId { get; set; }
        public Market Market { get; set; } = null!;

        public DateTime Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int Capacity { get; set; } = 10;
        public int BookedCount { get; set; } = 0;

        public bool HasFreeCapacity() => BookedCount < Capacity;
    }
}
