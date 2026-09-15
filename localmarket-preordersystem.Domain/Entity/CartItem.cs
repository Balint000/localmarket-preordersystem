using System;
using System.Collections.Generic;
using System.Text;

namespace localmarket_preordersystem.Domain.Entity
{
    public class CartItem
    {
        public int Id { get; private set; }
        public int CartId { get; set; }
        public Cart Cart { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPriceSnapshot{ get; set; } // a kosárba tételkori ár
    }
}
