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
        [Range(0, 10)]
        public int Place { get; set; } = 0;
        public List<Product?> Products { get; set; } = new();

    }
}
