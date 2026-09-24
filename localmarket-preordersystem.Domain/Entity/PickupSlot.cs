using localmarket_preordersystem.Domain.Exceptions;

namespace localmarket_preordersystem.Domain.Entity
{
    public class PickupSlot
    {
        public int Id { get; set; }
        public int ProducerId { get; private set; }
        public Producer Producer { get; private set; } = null!;

        public DateTime Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int Capacity { get; set; } = 10;
        public int BookedCount { get; set; } = 0;

        private PickupSlot()
        {
            // EF Core-nak kell
        }

        /// <summary>
        /// Létrehozáskor ellenőrzi, hogy a megadott nap szerepel-e a piac nyitvatartási
        /// napjai között  ez egy másik aggregate (Market) adatára támaszkodó invariáns,
        /// amit itt, létrehozáskor, nem futásidőben tartott élő referenciával.
        /// </summary>
        public static PickupSlot Create(Producer producer, DateTime date, TimeOnly startTime, TimeOnly endTime, int capacity)
        {
            ArgumentNullException.ThrowIfNull(producer);
            if (producer.Market is null)
                throw new DomainException("Az árushoz nincs betöltve a piac adata, az átvételi idősáv nem ellenőrizhető.");
            if (!producer.Market.OpeningDays.Contains(date.DayOfWeek))
                throw new DomainException($"A piac ezen a napon ({date.DayOfWeek}) nem tart nyitva.");
            if (endTime <= startTime)
                throw new DomainException("Az átvételi időpont vége nem lehet korábbi vagy egyenlő a kezdésnél.");
            if (capacity <= 0)
                throw new DomainException("Az átvételi időpont kapacitásának pozitívnak kell lennie.");

            return new PickupSlot
            {
                ProducerId = producer.Id,
                Producer = producer,
                Date = date.Date,
                StartTime = startTime,
                EndTime = endTime,
                Capacity = capacity,
                BookedCount = 0
            };
        }

        public bool HasFreeCapacity() => BookedCount < Capacity;

        /// <summary>
        /// Egy hely lefoglalása (pl. új Order létrehozásakor). Konkurens foglalás elleni
        /// védelemhez EF Core szinten optimistic concurrency token is kell majd (
        /// </summary>
        public void Book()
        {
            if (!HasFreeCapacity())
                throw new DomainException("Ez az átvételi időpont betelt, nem foglalható le további rendelés.");

            BookedCount++;
        }

        public void Release()
        {
            if (BookedCount == 0)
                return;

            BookedCount--;
        }
    }
}
