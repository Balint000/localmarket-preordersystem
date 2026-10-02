using System;
using System.Collections.Generic;
using System.Text;

namespace localmarket_preordersystem.Domain.Entity
{
    public class OrderItemStatusEntity
    {
        public int Id { get; set; }
        public int OrderItemId { get; set; }
        public OrderItem OrderItem { get; set; } = null!;

    }
}
