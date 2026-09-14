using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace localmarket_preordersystem.Domain.Entity
{
    public class Producer
    {
        public int Id { get; private set; }
        public string Name { get; set; } = string.Empty;

        public int MarketId { get; set; }
        public Market Market { get; set; } = null!;

        [Range(0, 10)]
        public int StallNumber { get; set; } = 0;
        public List<Product?> Products { get; set; } = new();

    }
}
