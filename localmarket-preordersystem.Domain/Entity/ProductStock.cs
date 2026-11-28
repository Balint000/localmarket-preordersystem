using localmarket_preordersystem.Domain.Exceptions;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Domain.Entity
{
    public class ProductStock
    {
        public int Id { get; private set; }

        public int ProductId { get; private set; }
        public Product Product { get; private set; } = null!;

        /// <summary>A hét hétfői napja. Mindig normalizált (ld. GetWeekStart).</summary>
        public DateTime WeekStartDate { get; private set; }

        // 0 = "erre a hétre elfogyott / nem kínálom" (a CK_ProductStocks_Quantity megszorítás is >= 0-t enged).
        public decimal Quantity { get; private set; }

        public decimal ReservedQuantity { get; private set; }

        public decimal AvailableQuantity => Quantity - ReservedQuantity;

        public Units Unit { get; private set; }

        public decimal UnitPrice { get; private set; }

        public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;

        private ProductStock()
        {
            // EF Core-nak kell
        }

        /// <summary>
        /// Egy dátumhoz tartozó hét hétfője. A heti kínálat és az átvételi idősáv így
        /// kapcsolódik: az idősáv dátumából a hét hétfője adja a ProductStock kulcsát.
        /// </summary>
        public static DateTime GetWeekStart(DateTime date)
        {
            var day = date.Date;
            var offset = ((int)day.DayOfWeek + 6) % 7; // hétfő = 0 ... vasárnap = 6
            return day.AddDays(-offset);
        }

        public static ProductStock Create(Product product, DateTime weekStartDate, decimal quantity, Units unit, decimal unitPrice)
        {
            ArgumentNullException.ThrowIfNull(product);
            if (quantity < 0)
                throw new DomainException("A heti mennyiség nem lehet negatív.");
            if (unitPrice <= 0)
                throw new DomainException("Az egységárnak pozitívnak kell lennie.");
            if (!Enum.IsDefined(unit))
                throw new DomainException("Érvénytelen mértékegység.");

            return new ProductStock
            {
                ProductId = product.Id,
                Product = product,
                WeekStartDate = GetWeekStart(weekStartDate),
                Quantity = quantity,
                ReservedQuantity = 0,
                Unit = unit,
                UnitPrice = unitPrice,
                LastUpdated = DateTime.UtcNow
            };
        }

        public void Update(DateTime weekStartDate, decimal quantity, Units unit, decimal unitPrice)
        {
            if (quantity < 0)
                throw new DomainException("A heti mennyiség nem lehet negatív.");
            if (unitPrice <= 0)
                throw new DomainException("Az egységárnak pozitívnak kell lennie.");
            if (!Enum.IsDefined(unit))
                throw new DomainException("Érvénytelen mértékegység.");
            if (quantity < ReservedQuantity)
                throw new DomainException("A készlet nem lehet kisebb a már lefoglalt mennyiségnél.");
            if (unit != Unit && ReservedQuantity > 0)
                throw new DomainException("A mértékegység nem módosítható, amíg van lefoglalt mennyiség.");

            WeekStartDate = GetWeekStart(weekStartDate);
            Quantity = quantity;
            Unit = unit;
            UnitPrice = unitPrice;
            LastUpdated = DateTime.UtcNow;
        }

        public void Reserve(decimal quantity)
        {
            if (quantity <= 0)
                throw new DomainException("A lefoglalandó mennyiségnek pozitívnak kell lennie.");
            if (decimal.Round(quantity, 3) != quantity)
                throw new DomainException("A mennyiség legfeljebb három tizedesjegyet tartalmazhat.");
            if (!Unit.IsValidOrderQuantity(quantity))
                throw new DomainException($"Ennél a terméknél a mennyiség {Unit.OrderStep():0.###} egységenként rendelhető (pl. {Unit.OrderStep():0.###}, {Unit.OrderStep() * 2:0.###}, {Unit.OrderStep() * 3:0.###}).");
            if (quantity > AvailableQuantity)
                throw new DomainException("Nincs elég elérhető készlet ehhez a mennyiséghez.");

            ReservedQuantity += quantity;
            LastUpdated = DateTime.UtcNow;
        }

        public void Release(decimal quantity)
        {
            if (quantity <= 0)
                throw new DomainException("A felszabadítandó mennyiségnek pozitívnak kell lennie.");

            ReservedQuantity = Math.Max(0, ReservedQuantity - quantity);
            LastUpdated = DateTime.UtcNow;
        }
    }
}
