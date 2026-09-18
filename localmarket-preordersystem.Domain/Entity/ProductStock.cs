using localmarket_preordersystem.Domain.Exceptions;
using localmarket_preordersystem.Domain.ValueObject;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace localmarket_preordersystem.Domain.Entity
{
    public class ProductStock
    {
        public int Id { get; private set; }

        public int ProductId { get; private set; }
        public Product Product { get; private set; } = null!;

        public DateTime WeekStartDate{ get; set; }

        [Range(0.01, 1000000)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; private set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ReservedQuantity { get; set; } = 0;

        public decimal AvailableQuantity => Quantity - ReservedQuantity;

        public Units Unit { get; private set; }

        [Range(0.01, 1000000)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } = decimal.Zero;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        private ProductStock()
        {
            // EF Core-nak 
        }

        public static ProductStock Create(Product product, DateTime weekStartDate, decimal quantity, Units unit, decimal unitPrice)
        {
            ArgumentNullException.ThrowIfNull(product);
            if (quantity <= 0)
                throw new DomainException("A heti mennyiségnek pozitívnak kell lennie.");
            if (unitPrice <= 0)
                throw new DomainException("Az egységárnak pozitívnak kell lennie.");

            return new ProductStock
            {
                ProductId = product.Id,
                Product = product,
                WeekStartDate = weekStartDate.Date,
                Quantity = quantity,
                ReservedQuantity = 0,
                Unit = unit,
                UnitPrice = unitPrice,
                LastUpdated = DateTime.UtcNow
            };
        }

        public void Reserve(decimal quantity)
        {
            if (quantity <= 0)
                throw new DomainException("A lefoglalandó mennyiségnek pozitívnak kell lennie.");
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
