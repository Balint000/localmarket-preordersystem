using localmarket_preordersystem.Domain.Exceptions;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Domain.Entity
{
    /// <summary>
    /// Egy vevő egy árusnál leadott rendelése egy átvételi idősávra. A többárusos kosárból
    /// árusonként egy Order lesz. Készletet és idősáv-helyet a leadáskor foglal (all-or-nothing).
    /// </summary>
    public class Order
    {
        private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
        {
            [OrderStatus.Pending] = [OrderStatus.Accepted, OrderStatus.PartiallyFulfilled, OrderStatus.Rejected, OrderStatus.Cancelled],
            [OrderStatus.Accepted] = [OrderStatus.Ready, OrderStatus.PickedUp, OrderStatus.Cancelled, OrderStatus.NotPickedUp, OrderStatus.Disputed],
            [OrderStatus.PartiallyFulfilled] = [OrderStatus.Ready, OrderStatus.PickedUp, OrderStatus.Cancelled, OrderStatus.NotPickedUp, OrderStatus.Disputed],
            [OrderStatus.Ready] = [OrderStatus.PickedUp, OrderStatus.Cancelled, OrderStatus.NotPickedUp, OrderStatus.Disputed],
            [OrderStatus.PickedUp] = [OrderStatus.Disputed],
            [OrderStatus.NotPickedUp] = [OrderStatus.Disputed],
            [OrderStatus.Disputed] = [OrderStatus.PickedUp, OrderStatus.Cancelled],
            [OrderStatus.Cancelled] = [],
            [OrderStatus.Rejected] = [],
        };

        public int Id { get; private set; }
        public int UserId { get; private set; }
        public User User { get; private set; } = null!;

        public int ProducerId { get; private set; }

        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public OrderStatus Status { get; private set; } = OrderStatus.Pending;

        private readonly List<OrderItem> _items = new();
        public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

        public int PickupSlotId { get; private set; }
        public PickupSlot PickupSlot { get; private set; } = null!;

        public string? DisputeNote { get; private set; }
        public string? RejectionReason { get; private set; }
        public Payment? Payment { get; private set; }

        public decimal TotalAmount => _items.Sum(i => i.Total);

        /// <summary>Foglal-e a rendelés készletet/idősáv-helyet (a lezáratlan, aktív állapotok).</summary>
        public bool HoldsReservations =>
            Status is OrderStatus.Pending or OrderStatus.Accepted or OrderStatus.PartiallyFulfilled or OrderStatus.Ready;

        private Order()
        {
            // EF Core-nak kell
        }

        /// <summary>
        /// Rendelés leadása: lefoglalja az idősáv egy helyét és a tételek készletét. Bármelyik
        /// invariáns sérülésekor DomainException; a hívó nem menti a részleges állapotot.
        /// </summary>
        public static Order Place(User customer, PickupSlot slot, IEnumerable<(Product Product, decimal Quantity)> lines, DateTime now)
        {
            ArgumentNullException.ThrowIfNull(customer);
            ArgumentNullException.ThrowIfNull(slot);
            ArgumentNullException.ThrowIfNull(lines);
            var list = lines.ToList();
            if (list.Count == 0)
                throw new DomainException("A rendelésnek legalább egy tételt tartalmaznia kell.");
            if (list.Select(l => l.Product.Id).Distinct().Count() != list.Count)
                throw new DomainException("Egy termék csak egyszer szerepelhet a rendelésben.");

            var order = new Order
            {
                UserId = customer.Id,
                User = customer,
                ProducerId = slot.ProducerId,
                PickupSlotId = slot.Id,
                PickupSlot = slot,
                CreatedAt = now,
                Status = OrderStatus.Pending
            };

            slot.Book(now);

            foreach (var (product, quantity) in list)
            {
                if (product.ProducerId != slot.ProducerId)
                    throw new DomainException($"A(z) '{product.Name}' termék nem ennek az árusnak a kínálatából való.");
                if (quantity <= 0)
                    throw new DomainException("A rendelt mennyiségnek pozitívnak kell lennie.");

                var stock = product.ReserveStockForPickup(slot.Date, quantity);
                order._items.Add(OrderItem.Create(product, stock, quantity));
            }

            return order;
        }

        /// <summary>A jelenleg lefoglalva tartott tételek (felszabadításhoz).</summary>
        public IReadOnlyList<ReservedItem> GetHeldReservations() =>
            HoldsReservations
                ? _items.Where(i => i.FulfilledQuantity > 0).Select(i => new ReservedItem(i.ProductId, i.FulfilledQuantity)).ToList()
                : new List<ReservedItem>();

        public void SetPayment(PaymentMethod method)
        {
            if (Payment is not null)
                throw new DomainException("A rendeléshez már tartozik fizetés.");
            Payment = Payment.Create(this, TotalAmount, method);
        }

        public void Accept() => Transition(OrderStatus.Accepted);

        /// <summary>
        /// Részleges teljesítés: tételenként a vállalt mennyiség (hiányzó tétel = 0).
        /// Visszaadja a felszabadítandó különbözetet.
        /// </summary>
        public IReadOnlyList<ReservedItem> AcceptPartially(IReadOnlyDictionary<int, decimal> fulfilledByProductId)
        {
            ArgumentNullException.ThrowIfNull(fulfilledByProductId);
            EnsureCanMoveTo(OrderStatus.PartiallyFulfilled);

            foreach (var key in fulfilledByProductId.Keys)
                if (_items.All(i => i.ProductId != key))
                    throw new DomainException("A megadott termék nem szerepel a rendelésben.");

            var released = new List<ReservedItem>();
            var anyFulfilled = false;
            var anyShort = false;
            foreach (var item in _items)
            {
                var fulfilled = fulfilledByProductId.TryGetValue(item.ProductId, out var f) ? f : 0m;
                var shortfall = item.SetFulfilled(fulfilled);
                if (shortfall > 0) released.Add(new ReservedItem(item.ProductId, shortfall));
                anyFulfilled |= fulfilled > 0;
                anyShort |= fulfilled < item.Quantity;
            }

            if (!anyFulfilled)
                throw new DomainException("Részleges teljesítésnél legalább egy tételből kell vállalni; különben utasítsd el a rendelést.");
            if (!anyShort)
                throw new DomainException("Minden tétel teljes mennyisége vállalva van, használd az elfogadást.");

            Status = OrderStatus.PartiallyFulfilled;
            if (Payment is { Status: PaymentStatus.Pending })
                Payment.Amount = TotalAmount;
            return released;
        }

        public IReadOnlyList<ReservedItem> Reject(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainException("Elutasításhoz indoklás megadása kötelező.");
            var held = GetHeldReservations();
            Transition(OrderStatus.Rejected);
            RejectionReason = reason.Trim();
            Payment?.Cancel();
            return held;
        }

        public IReadOnlyList<ReservedItem> Cancel()
        {
            var held = GetHeldReservations();
            Transition(OrderStatus.Cancelled);
            if (Payment is { Status: PaymentStatus.Pending or PaymentStatus.Error })
                Payment.Cancel();
            return held;
        }

        public void MarkReady() => Transition(OrderStatus.Ready);

        public void MarkPickedUp()
        {
            Transition(OrderStatus.PickedUp);
            if (Payment is { Status: PaymentStatus.Pending })
                Payment.MarkCompleted();
        }

        public void MarkNotPickedUp() => Transition(OrderStatus.NotPickedUp);

        public void Dispute(string note)
        {
            if (string.IsNullOrWhiteSpace(note))
                throw new DomainException("Vitatáshoz indoklás megadása kötelező.");
            Transition(OrderStatus.Disputed);
            DisputeNote = note.Trim();
        }

        /// <summary>Piacvezetői döntés: a vitatott rendelés átvettnek számít.</summary>
        public void ResolveDisputeAsPickedUp(string note)
        {
            if (Status != OrderStatus.Disputed)
                throw new DomainException("Csak vitatott rendelés zárható le.");
            Transition(OrderStatus.PickedUp);
            DisputeNote = AppendNote(note);
        }

        /// <summary>Piacvezetői döntés: a vitatott rendelés lemondásra kerül.</summary>
        public void ResolveDisputeAsCancelled(string note)
        {
            if (Status != OrderStatus.Disputed)
                throw new DomainException("Csak vitatott rendelés zárható le.");
            Transition(OrderStatus.Cancelled);
            DisputeNote = AppendNote(note);
            if (Payment is { Status: PaymentStatus.Completed })
                Payment.Refund();
            else if (Payment is { Status: PaymentStatus.Pending or PaymentStatus.Error })
                Payment.Cancel();
        }

        private string AppendNote(string note) =>
            string.IsNullOrWhiteSpace(note) ? DisputeNote ?? string.Empty : $"{DisputeNote} | Döntés: {note.Trim()}";

        private void EnsureCanMoveTo(OrderStatus target)
        {
            if (!AllowedTransitions[Status].Contains(target))
                throw new DomainException($"A rendelés '{Status}' állapotból nem vihető '{target}' állapotba.");
        }

        private void Transition(OrderStatus target)
        {
            EnsureCanMoveTo(target);
            Status = target;
        }
    }
}
