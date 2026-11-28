using localmarket_preordersystem.Domain.Exceptions;

namespace localmarket_preordersystem.Domain.Entity
{
    public class PickupSlot
    {
        public int Id { get; private set; }
        public int ProducerId { get; private set; }
        public Producer Producer { get; private set; } = null!;

        public DateTime Date { get; private set; }
        public TimeOnly StartTime { get; private set; }
        public TimeOnly EndTime { get; private set; }
        public int Capacity { get; private set; } = 10;
        public int BookedCount { get; private set; } = 0;

        private PickupSlot()
        {
            // EF Core-nak kell
        }

        /// <summary>
        /// Létrehozáskor ellenőrzi, hogy a megadott nap szerepel-e a piac nyitvatartási
        /// napjai között — ez egy másik aggregate (Market) adatára támaszkodó invariáns,
        /// amit itt, létrehozáskor, nem futásidőben tartott élő referenciával.
        /// </summary>
        public static PickupSlot Create(Producer producer, DateTime date, TimeOnly startTime, TimeOnly endTime, int capacity)
        {
            ArgumentNullException.ThrowIfNull(producer);
            if (!producer.CanListProducts())
                throw new DomainException("Csak jóváhagyott árus hirdethet átvételi időpontot.");
            if (producer.Market is null)
                throw new DomainException("Az árushoz nincs betöltve a piac adata, az átvételi idősáv nem ellenőrizhető.");
            if (!producer.Market.OpeningDays.Contains(date.DayOfWeek))
                throw new DomainException($"A piac ezen a napon ({date.DayOfWeek}) nem tart nyitva.");
            if (endTime <= startTime)
                throw new DomainException("Az átvételi időpont vége nem lehet korábbi vagy egyenlő a kezdésnél.");
            if (!producer.Market.CoversTime(startTime, endTime))
                throw OutsideHours(producer.Market);
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

        private static DomainException OutsideHours(Market market) =>
            new($"Az átvételi időpont a piac nyitvatartásán belül lehet ({market.OpensAt:HH\\:mm}–{market.ClosesAt:HH\\:mm}).");

        /// <summary>Foglalható-e most az idősáv (nem járt le, van hely, az árus aktív, a piac aznap nyitva van).</summary>
        public bool IsBookable(DateTime now) =>
            !HasEnded(now) && HasFreeCapacity()
            && Producer?.Market is { IsActive: true } market
            && Producer.CanListProducts()
            && market.OpeningDays.Contains(Date.DayOfWeek)
            && market.CoversTime(StartTime, EndTime);

        public bool HasEnded(DateTime now) => Date.Date.Add(EndTime.ToTimeSpan()) <= now;

        /// <summary>
        /// Átfedi-e ez az idősáv a megadott napi időablakot. Az egymáshoz csak érintkező sávok
        /// (pl. 8–10 és 10–12) nem számítanak átfedésnek.
        /// </summary>
        public bool Overlaps(DateTime date, TimeOnly startTime, TimeOnly endTime)
            => Date.Date == date.Date && StartTime < endTime && startTime < EndTime;

        /// <summary>
        /// Egy hely lefoglalása (pl. új Order létrehozásakor). Konkurens foglalás ellen a
        /// BookedCount optimistic concurrency token (ld. PickupSlotConfiguration).
        /// </summary>
        public void Book(DateTime now)
        {
            if (HasEnded(now))
                throw new DomainException("Ez az átvételi időpont már lejárt.");
            // a meghirdetés óta a piac vagy az árus állapota változhatott: foglaláskor újra ellenőrizzük
            var market = Producer?.Market
                ?? throw new DomainException("Az idősáv árusa vagy piaca nincs betöltve, a foglalás nem ellenőrizhető.");
            if (!Producer.CanListProducts())
                throw new DomainException("Ez az árus jelenleg nem fogad rendelést.");
            if (!market.IsActive)
                throw new DomainException("A piac jelenleg szünetel, nem fogad rendelést.");
            if (!market.OpeningDays.Contains(Date.DayOfWeek))
                throw new DomainException("A piac ezen a napon már nem tart nyitva, ez az időpont nem foglalható.");
            if (!market.CoversTime(StartTime, EndTime))
                throw new DomainException("Ez az időpont kívül esik a piac jelenlegi nyitvatartásán, nem foglalható.");
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

        /// <summary>
        /// Az idősáv módosítása. Lejárt idősáv nem módosítható; a kapacitás nem mehet a foglalások
        /// száma alá; foglalt idősáv napja/időablaka nem mozgatható (a vevők erre az időre foglaltak).
        /// A Producer.Market betöltése szükséges (nyitvatartási nap ellenőrzés).
        /// </summary>
        public void Update(DateTime date, TimeOnly startTime, TimeOnly endTime, int capacity, DateTime now)
        {
            if (HasEnded(now))
                throw new DomainException("A lejárt átvételi időpont nem módosítható.");
            if (endTime <= startTime)
                throw new DomainException("Az átvételi időpont vége nem lehet korábbi vagy egyenlő a kezdésnél.");
            if (capacity <= 0)
                throw new DomainException("Az átvételi időpont kapacitásának pozitívnak kell lennie.");
            if (capacity < BookedCount)
                throw new DomainException($"A kapacitás nem lehet kisebb a már lefoglalt helyek számánál ({BookedCount}).");

            var newDate = date.Date;
            var timeChanged = newDate != Date || startTime != StartTime || endTime != EndTime;
            if (timeChanged)
            {
                if (BookedCount > 0)
                    throw new DomainException("A foglalt átvételi időpont napja és időablaka nem módosítható, csak a kapacitás.");
                if (Producer?.Market is null)
                    throw new DomainException("Az árushoz nincs betöltve a piac adata, az átvételi idősáv nem ellenőrizhető.");
                if (!Producer.Market.OpeningDays.Contains(newDate.DayOfWeek))
                    throw new DomainException($"A piac ezen a napon ({newDate.DayOfWeek}) nem tart nyitva.");
                if (!Producer.Market.CoversTime(startTime, endTime))
                    throw OutsideHours(Producer.Market);
                if (newDate.Add(endTime.ToTimeSpan()) <= now)
                    throw new DomainException("Múltbeli időpont nem hirdethető meg.");
            }

            Date = newDate;
            StartTime = startTime;
            EndTime = endTime;
            Capacity = capacity;
        }

        /// <summary>Foglalt idősáv nem törölhető (a rendeléseket előbb le kell mondani / el kell utasítani).</summary>
        public void EnsureCanBeRemoved()
        {
            if (BookedCount > 0)
                throw new DomainException("A foglalással rendelkező átvételi időpont nem törölhető.");
        }
    }
}
