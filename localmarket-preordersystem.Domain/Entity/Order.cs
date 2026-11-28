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
        // Vita: átvétel / „nem jött el” után bármikor, illetve elfogadott, de lezáratlan rendelésnél az átvételi
        // idősáv vége után (pl. az árus nem adta ki és nem is jelölte). Átadás előtt nem nyitható, így a vita
        // lezárása sem hagyhat élő (jövőbeli) foglalást: a lejárt idősáv foglalása már nem számít.
        private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
        {
            [OrderStatus.Pending] = [OrderStatus.Accepted, OrderStatus.PartiallyFulfilled, OrderStatus.Rejected, OrderStatus.Cancelled, OrderStatus.Expired],
            [OrderStatus.Accepted] = [OrderStatus.Ready, OrderStatus.PickedUp, OrderStatus.Cancelled, OrderStatus.NotPickedUp, OrderStatus.Disputed],
            [OrderStatus.PartiallyFulfilled] = [OrderStatus.Ready, OrderStatus.PickedUp, OrderStatus.Cancelled, OrderStatus.NotPickedUp, OrderStatus.Disputed],
            [OrderStatus.Ready] = [OrderStatus.PickedUp, OrderStatus.Cancelled, OrderStatus.NotPickedUp, OrderStatus.Disputed],
            [OrderStatus.PickedUp] = [OrderStatus.Disputed],
            [OrderStatus.NotPickedUp] = [OrderStatus.Disputed],
            [OrderStatus.Disputed] = [OrderStatus.PickedUp, OrderStatus.Cancelled],
            [OrderStatus.Cancelled] = [],
            [OrderStatus.Rejected] = [],
            [OrderStatus.Expired] = [],
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
                // A `now` helyi idő (az idősáv helyi dátumához/időpontjához hasonlítjuk); az adatbázisba
                // (timestamptz) csak UTC kerülhet, ahogy a többi entitásnál is.
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending
            };

            slot.Book(now);

            foreach (var (product, quantity) in list)
            {
                if (product.ProducerId != slot.ProducerId)
                    throw new DomainException($"A(z) '{product.Name}' termék nem ennek az árusnak a kínálatából való.");
                if (quantity <= 0)
                    throw new DomainException("A rendelt mennyiségnek pozitívnak kell lennie.");
                if (product.IsArchived)
                    throw new DomainException($"A(z) '{product.Name}' termék már nem rendelhető.");

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

        /// <summary>Teljes elfogadás: minden tétel a rendelt mennyiségben teljesül. Lejárt idősávra már nem.</summary>
        public void Accept(DateTime now)
        {
            EnsurePickupNotEnded(now, "Lejárt átvételi időpontú rendelés már nem fogadható el.");
            Transition(OrderStatus.Accepted);
            foreach (var item in _items)
                item.SetFulfilled(item.Quantity);
        }

        /// <summary>
        /// Részleges teljesítés: tételenként a vállalt mennyiség (hiányzó tétel = 0).
        /// Visszaadja a felszabadítandó különbözetet.
        /// </summary>
        public IReadOnlyList<ReservedItem> AcceptPartially(IReadOnlyDictionary<int, decimal> fulfilledByProductId, DateTime now)
        {
            ArgumentNullException.ThrowIfNull(fulfilledByProductId);
            EnsureCanMoveTo(OrderStatus.PartiallyFulfilled);
            EnsurePickupNotEnded(now, "Lejárt átvételi időpontú rendelés már nem fogadható el.");

            foreach (var key in fulfilledByProductId.Keys)
                if (_items.All(i => i.ProductId != key))
                    throw new DomainException("A megadott termék nem szerepel a rendelésben.");

            // előbb csak ellenőrzünk, hogy hibánál ne maradjon félig módosított tétel
            var plan = _items
                .Select(i => (Item: i, Fulfilled: fulfilledByProductId.TryGetValue(i.ProductId, out var f) ? f : 0m))
                .ToList();
            var invalid = plan.FirstOrDefault(p => p.Fulfilled < 0 || p.Fulfilled > p.Item.Quantity);
            if (invalid.Item is not null)
                throw new DomainException($"A(z) '{invalid.Item.ProductName}' teljesített mennyisége 0 és {invalid.Item.Quantity} között lehet.");
            if (plan.All(p => p.Fulfilled <= 0))
                throw new DomainException("Részleges teljesítésnél legalább egy tételből kell vállalni; különben utasítsd el a rendelést.");
            if (plan.All(p => p.Fulfilled >= p.Item.Quantity))
                throw new DomainException("Minden tétel teljes mennyisége vállalva van, használd az elfogadást.");

            var released = new List<ReservedItem>();
            foreach (var (item, fulfilled) in plan)
            {
                var shortfall = item.SetFulfilled(fulfilled);
                if (shortfall > 0) released.Add(new ReservedItem(item.ProductId, shortfall));
            }

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

        /// <summary>
        /// Vevői lemondás. A még el nem fogadott rendelés az idősáv kezdetéig mondható le; az elfogadott
        /// (az árus már készül rá) csak a lemondási határidőig: az idősáv kezdete előtt <paramref name="cutoff"/>-fal.
        /// </summary>
        public IReadOnlyList<ReservedItem> CancelByCustomer(DateTime now, TimeSpan cutoff)
        {
            EnsureNotDisputed();
            EnsureCanMoveTo(OrderStatus.Cancelled);
            var start = PickupStartsAt();
            var deadline = Status == OrderStatus.Pending ? start : start - cutoff;
            if (now >= deadline)
                throw new DomainException(Status == OrderStatus.Pending
                    ? "Az átvételi időpont már elkezdődött, a rendelés nem mondható le."
                    : $"A lemondási határidő ({deadline:yyyy-MM-dd HH:mm}) lejárt. Ha mégsem tudsz menni, jelezd az árusnak.");
            return CancelCore();
        }

        /// <summary>
        /// Az árus vagy a piacvezető lemondja az aktív rendelést (pl. elfagyott a termés, elmarad a piacnap).
        /// Indoklás kötelező, a vevő látja. Az idősáv vége után már nem: ott átvétel / nem átvétel / vita a lezárás.
        /// </summary>
        public IReadOnlyList<ReservedItem> CancelBySeller(DateTime now, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainException("Az árusi vagy piacvezetői lemondáshoz indoklás megadása kötelező.");
            EnsureNotDisputed();
            EnsureCanMoveTo(OrderStatus.Cancelled);
            EnsurePickupNotEnded(now, "Lezárult átvételi időpont után a rendelés nem mondható le: jelöld átvettnek vagy nem átvettnek.");
            var held = CancelCore();
            RejectionReason = reason.Trim();
            return held;
        }

        /// <summary>
        /// Automatikus lezárás: az árus az idősáv végéig nem válaszolt a függő rendelésre. A foglalás felszabadul.
        /// </summary>
        public IReadOnlyList<ReservedItem> Expire(DateTime now)
        {
            if (Status != OrderStatus.Pending)
                throw new DomainException("Csak függő rendelés járhat le.");
            if (!PickupSlot.HasEnded(now))
                throw new DomainException("A rendelés még nem járt le: az átvételi időpont még nem ért véget.");
            var held = GetHeldReservations();
            Transition(OrderStatus.Expired);
            Payment?.Cancel();
            return held;
        }

        private IReadOnlyList<ReservedItem> CancelCore()
        {
            var held = GetHeldReservations();
            Transition(OrderStatus.Cancelled);
            if (Payment is { Status: PaymentStatus.Pending or PaymentStatus.Error })
                Payment.Cancel();
            return held;
        }

        public void MarkReady(DateTime now)
        {
            EnsurePickupNotEnded(now, "Lejárt átvételi időpontú rendelés már nem jelölhető átvehetőnek.");
            Transition(OrderStatus.Ready);
        }

        /// <summary>Átadás rögzítése: legkorábban az átvétel napján (előre, véletlenül ne lehessen lezárni).</summary>
        public void MarkPickedUp(DateTime now)
        {
            EnsureNotDisputed();
            EnsureCanMoveTo(OrderStatus.PickedUp);
            if (now < PickupSlot.Date.Date)
                throw new DomainException("Átvettnek legkorábban az átvétel napján jelölhető.");
            Transition(OrderStatus.PickedUp);
            if (Payment is { Status: PaymentStatus.Pending })
                Payment.MarkCompleted();
        }

        /// <summary>„Nem jött el”: csak az átvételi idősáv vége után (addig a vevő még megérkezhet).</summary>
        public void MarkNotPickedUp(DateTime now)
        {
            EnsureCanMoveTo(OrderStatus.NotPickedUp);
            if (!PickupSlot.HasEnded(now))
                throw new DomainException("„Nem jött el” csak az átvételi időpont vége után jelölhető.");
            Transition(OrderStatus.NotPickedUp);
        }

        public void Dispute(string note, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(note))
                throw new DomainException("Vitatáshoz indoklás megadása kötelező.");
            EnsureCanMoveTo(OrderStatus.Disputed);
            if (HoldsReservations && !PickupSlot.HasEnded(now))
                throw new DomainException("Átadás előtt nem nyitható vita: ha nem tudsz menni, mondd le a rendelést. Ha az átadás elmarad, az átvételi idő vége után jelezheted.");
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
            // a függőben maradt fizetés az átvétellel teljesítettnek számít (mint a MarkPickedUp-nál)
            if (Payment is { Status: PaymentStatus.Pending })
                Payment.MarkCompleted();
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

        private DateTime PickupStartsAt() => PickupSlot.Date.Date.Add(PickupSlot.StartTime.ToTimeSpan());

        private void EnsurePickupNotEnded(DateTime now, string message)
        {
            if (PickupSlot.HasEnded(now))
                throw new DomainException(message);
        }

        private void EnsureNotDisputed()
        {
            if (Status == OrderStatus.Disputed)
                throw new DomainException("A vitatott rendelésről a piacvezető dönt; a rendelés más módon nem zárható le.");
        }

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
