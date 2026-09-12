using System;
using System.Collections.Generic;
using System.Text;

namespace localmarket_preordersystem.Domain.Entity
{
    public class Cart
    {
        public int Id { get; private set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public List<CartItem> Items { get; set; } = new();
    }
}
