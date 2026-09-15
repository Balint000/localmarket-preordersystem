using localmarket_preordersystem.Domain.ValueObject;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace localmarket_preordersystem.Domain.Entity
{
    public class Product
    {
        public int Id { get; private set; }

        [Required]
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        public ProductStock? Stock { get; set; }
        public int StockId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<Allergy?> Allergies { get; set; } = new();
        public int CategoryId { get; set; }
        public List<Category?> Category { get; set; } = new();

        public List<ProductStock> WeeklyStocks { get; set; } = new();
    }
}
