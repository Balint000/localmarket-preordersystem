namespace localmarket_preordersystem.Domain.Entity
{
    public class ProductStock
    {
        public int Id { get; private set; }

        public DateTime WeekStartDate{ get; set; }

        [Range(0.01, 1000000)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ReservedQuantity { get; set; } = 0;

        public decimal AvailableQuantity => Quantity - ReservedQuantity;

        public Units Unit { get; set; }

        [Range(0.01, 1000000)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } = decimal.Zero;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
