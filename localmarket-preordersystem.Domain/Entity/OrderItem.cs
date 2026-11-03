using localmarket_preordersystem.Domain.Exceptions;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Domain.Entity
{
    public class OrderItem
    {
        public int Id { get; private set; }
        public int OrderId { get; private set; }
        public Order Order { get; private set; } = null!;
        public int ProductId { get; private set; }
        public Product Product { get; private set; } = null!;

        // Snapshot a rendelés pillanatából: a későbbi ár-/névváltozás nem érinti.
        public string ProductName { get; private set; } = string.Empty;
        public Units Unit { get; private set; }
        public decimal UnitPrice { get; private set; }

        /// <summary>A megrendelt mennyiség.</summary>
        public decimal Quantity { get; private set; }

        /// <summary>Az árus által vállalt/teljesített mennyiség (kezdetben = Quantity).</summary>
        public decimal FulfilledQuantity { get; private set; }

        public decimal Total => UnitPrice * FulfilledQuantity;

        public OrderItemStatus Status { get; private set; } = OrderItemStatus.Pending;

        private OrderItem()
        {
            // EF Core-nak kell
        }

        internal static OrderItem Create(Product product, ProductStock stock, decimal quantity)
        {
            return new OrderItem
            {
                ProductId = product.Id,
                Product = product,
                ProductName = product.Name,
                Unit = stock.Unit,
                UnitPrice = stock.UnitPrice,
                Quantity = quantity,
                FulfilledQuantity = quantity,
                Status = OrderItemStatus.Pending
            };
        }

        /// <summary>Visszaadja a felszabadítandó (le nem szállított) mennyiséget.</summary>
        internal decimal SetFulfilled(decimal fulfilled)
        {
            if (fulfilled < 0 || fulfilled > Quantity)
                throw new DomainException($"A(z) '{ProductName}' teljesített mennyisége 0 és {Quantity} között lehet.");

            var shortfall = FulfilledQuantity - fulfilled;
            FulfilledQuantity = fulfilled;
            Status = fulfilled == Quantity ? OrderItemStatus.Fulfilled
                   : fulfilled == 0 ? OrderItemStatus.OutOfStock
                   : OrderItemStatus.PartiallyFulfilled;
            return shortfall;
        }
    }
}
