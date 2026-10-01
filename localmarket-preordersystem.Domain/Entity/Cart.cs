using localmarket_preordersystem.Domain.Exceptions;
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
        private readonly List<CartItem> _items = new();
        public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

        private Cart()
        {
            // EF Core-nak
        }

        public static Cart CreateEmpty(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            return new Cart
            {
                UserId = user.Id,
                User = user
            };
        }

        public void AddItem(Product product, decimal quantity, decimal currentUnitPrice)
        {
            ArgumentNullException.ThrowIfNull(product);
            if (quantity <= 0)
                throw new DomainException("A hozzáadott mennyiségnek pozitívnak kell lennie.");

            var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);
            if (existing is not null)
            {
                existing.IncreaseQuantity(quantity);
                return;
            }

            _items.Add(CartItem.Create(product, quantity, currentUnitPrice));
        }

        public void UpdateItemQuantity(int productId, decimal quantity)
        {
            var item = _items.FirstOrDefault(i => i.ProductId == productId)
                ?? throw new DomainException("Ez a termék nincs a kosárban.");

            item.SetQuantity(quantity);
        }

        public void RemoveItem(int productId) => _items.RemoveAll(i => i.ProductId == productId);

        public void Clear() => _items.Clear();

        public ILookup<int, CartItem> GroupItemsByProducer() => _items.ToLookup(i => i.Product.ProducerId);
    }
}
