using System.ComponentModel.DataAnnotations;
using localmarket_preordersystem.Domain.Exceptions;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Domain.Entity
{
    public class Product
    {
        public int Id { get; private set; }

        public int ProducerId { get; private set; }
        public Producer Producer { get; private set; } = null!;

        [MaxLength(100)]
        public string Name { get; private set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; private set; } = string.Empty;

        // Egy termék több kategóriába is tartozhat (N:N). Az EF Core konfiguráció
        private readonly List<Category> _categories = new();
        public IReadOnlyCollection<Category> Categories => _categories.AsReadOnly();

        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        /// <summary>A feltöltött termékkép fájlneve (a tárolóban); null, ha nincs kép.</summary>
        public string? ImageFileName { get; private set; }

        /// <summary>
        /// Archivált (törölt) termék: rendelési előzménye miatt nem törölhető fizikailag, de a kínálatban nem
        /// jelenik meg, nem tehető kosárba és nem rendelhető. A név újra felhasználható.
        /// </summary>
        public bool IsArchived { get; private set; }

        private readonly List<Allergy> _allergies = new();
        public IReadOnlyCollection<Allergy> Allergies => _allergies.AsReadOnly();

        private readonly List<ProductStock> _weeklyStocks = new();
        public IReadOnlyCollection<ProductStock> WeeklyStocks => _weeklyStocks.AsReadOnly();

        private Product()
        {
            // EF Core-nak kell
        }

        public static Product Create(Producer producer, string name, string description, IEnumerable<Category> categories, IEnumerable<Allergy>? allergies = null)
        {
            ArgumentNullException.ThrowIfNull(producer);
            if (!producer.CanListProducts())
                throw new DomainException("Csak jóváhagyott (Approved) árus hozhat létre terméket.");
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("A termék nevének megadása kötelező.");

            var distinctCategories = NormalizeCategories(categories);

            var product = new Product
            {
                ProducerId = producer.Id,
                Producer = producer,
                Name = name.Trim(),
                Description = description?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            product._categories.AddRange(distinctCategories);

            if (allergies is not null)
                product._allergies.AddRange(allergies.Distinct());

            return product;
        }

        public void UpdateDetails(string name, string description, IEnumerable<Category> categories, IEnumerable<Allergy> allergies)
        {
            EnsureNotArchived();
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("A termék nevének megadása kötelező.");

            var distinctCategories = NormalizeCategories(categories);
            ArgumentNullException.ThrowIfNull(allergies);

            Name = name.Trim();
            Description = description?.Trim() ?? string.Empty;

            _categories.Clear();
            _categories.AddRange(distinctCategories);

            _allergies.Clear();
            _allergies.AddRange(allergies.Distinct());
        }

        public void SetImage(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new DomainException("A kép fájlneve nem lehet üres.");

            ImageFileName = fileName;
        }

        public void ClearImage() => ImageFileName = null;

        public void Archive() => IsArchived = true;

        private void EnsureNotArchived()
        {
            if (IsArchived)
                throw new DomainException($"A(z) '{Name}' termék törölve (archiválva) van, nem módosítható és nem rendelhető.");
        }

        // Invariáns: legalább egy kategória kell (különben a termék a kategória-szűrésekben
        // sehol nem jelenne meg).
        private static List<Category> NormalizeCategories(IEnumerable<Category>? categories)
        {
            var distinct = categories?.Distinct().ToList() ?? new List<Category>();
            if (distinct.Count == 0)
                throw new DomainException("A terméknek legalább egy kategóriába tartoznia kell.");

            return distinct;
        }

        /// <summary>A megadott dátumot tartalmazó hét kínálatának rögzítése (a hét hétfőjére normalizálva).</summary>
        public void AddWeeklyStock(DateTime weekStartDate, decimal quantity, Units unit, decimal unitPrice)
        {
            EnsureNotArchived();
            var normalizedWeek = ProductStock.GetWeekStart(weekStartDate);
            if (_weeklyStocks.Any(s => s.WeekStartDate == normalizedWeek))
                throw new DomainException("Erre a hétre már van rögzített kínálat ehhez a termékhez.");

            _weeklyStocks.Add(ProductStock.Create(this, normalizedWeek, quantity, unit, unitPrice));
        }

        /// <summary>Már rögzített heti kínálat módosítása. A hét nem változtatható, csak a mennyiség, egység, ár.</summary>
        public void UpdateWeeklyStock(DateTime weekStartDate, decimal quantity, Units unit, decimal unitPrice)
        {
            EnsureNotArchived();
            var stock = GetStockForWeek(weekStartDate)
                ?? throw new DomainException("Erre a hétre nincs rögzített kínálat ennél a terméknél.");

            stock.Update(stock.WeekStartDate, quantity, unit, unitPrice);
        }

        /// <summary>A megadott dátumot tartalmazó hét kínálata (bármelyik napot megadhatod).</summary>
        public ProductStock? GetStockForWeek(DateTime date)
        {
            var normalizedWeek = ProductStock.GetWeekStart(date);
            return _weeklyStocks.FirstOrDefault(s => s.WeekStartDate == normalizedWeek);
        }

        /// <summary>
        /// A legközelebbi még rendelhető hét kínálata (ár- és mennyiségjelzéshez, kosárhoz). Az aktuális hét csak
        /// akkor számít, ha a piacon van még nyitvatartás a héten; különben a következő meghirdetett hét jön.
        /// (Ha a piac nincs betöltve, az aktuális hetet is figyelembe vesszük.)
        /// </summary>
        public ProductStock? GetNextAvailableStock(DateTime now)
        {
            if (IsArchived) return null;
            var currentWeek = ProductStock.GetWeekStart(now);
            var currentWeekOpen = Producer?.Market is not { } market || market.HasOpeningLeftInWeek(now);
            return _weeklyStocks
                .Where(s => s.WeekStartDate > currentWeek || (s.WeekStartDate == currentWeek && currentWeekOpen))
                .OrderBy(s => s.WeekStartDate)
                .FirstOrDefault();
        }

        /// <summary>
        /// A lezárt hetek kínálata (a hét utolsó piacnapja után), legújabb elöl: ebből látja az árus, mennyi maradt.
        /// </summary>
        public IEnumerable<ProductStock> GetClosedWeekStocks(DateTime now)
        {
            var currentWeek = ProductStock.GetWeekStart(now);
            var currentWeekOpen = Producer?.Market is not { } market || market.HasOpeningLeftInWeek(now);
            return _weeklyStocks
                .Where(s => s.WeekStartDate < currentWeek || (s.WeekStartDate == currentWeek && !currentWeekOpen))
                .OrderByDescending(s => s.WeekStartDate);
        }

        /// <summary>
        /// Készlet lefoglalása egy átvételi napra. A kínálat az átvételi nap hetéhez tartozik, így
        /// "nem rendelhető olyan termék, ami az adott héten nem elérhető" itt dől el.
        /// </summary>
        public ProductStock ReserveStockForPickup(DateTime pickupDate, decimal quantity)
        {
            EnsureNotArchived();
            var stock = GetStockForWeek(pickupDate)
                ?? throw new DomainException(
                    $"A(z) '{Name}' termék nem elérhető a(z) {pickupDate:yyyy-MM-dd} napot tartalmazó héten.");

            stock.Reserve(quantity);
            return stock;
        }

        /// <summary>Korábban lefoglalt készlet felszabadítása (lemondás, elutasítás, részleges teljesítés).</summary>
        public void ReleaseStockForPickup(DateTime pickupDate, decimal quantity)
        {
            GetStockForWeek(pickupDate)?.Release(quantity);
        }
    }
}
