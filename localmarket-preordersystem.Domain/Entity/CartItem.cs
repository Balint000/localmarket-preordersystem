using localmarket_preordersystem.Domain.Exceptions;
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

        private CartItem()
        {
            // EF Core-nak
        }

        internal static CartItem Create(Product product, int quantity, decimal unitPriceSnapshot)
        {
            ArgumentNullException.ThrowIfNull(product);
            if (quantity <= 0)
                throw new DomainException("A kosártétel mennyiségének pozitívnak kell lennie.");

            return new CartItem
            {
                ProductId = product.Id,
                Product = product,
                Quantity = quantity,
                UnitPriceSnapshot = unitPriceSnapshot
            };
        }

        internal void IncreaseQuantity(int amount)
        {
            if (amount <= 0)
                throw new DomainException(
                    "A növelendő mennyiségnek pozitívnak kell lennie.");

            Quantity += amount;
        }

        internal void DecreaseQuantity(int amount)
        {
            if (amount <= 0)
                throw new DomainException(
                    "A csökkentendő mennyiségnek pozitívnak kell lennie.");

            if (amount >= Quantity)
                throw new DomainException(
                    "A kosártétel mennyisége nem csökkenhet nullára.");

            Quantity -= amount;
        }

        internal void SetQuantity(int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("A kosártétel mennyiségének pozitívnak kell lennie.");

            Quantity = quantity;
        }
    }
}
