using localmarket_preordersystem.Domain.ValueObject;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace localmarket_preordersystem.Domain.Entity
{
    public class ProductStock
    {
        public int Id { get; private set; }

        [Range(0.01, 1000000)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; } = 0;
        public Units Unit { get; set; }

        [Range(0.01, 1000000)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } = decimal.Zero;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
