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

        // Invariáns: legalább egy kategória kell (különben a termék a kategória-szűrésekben
        // sehol nem jelenne meg).
        private static List<Category> NormalizeCategories(IEnumerable<Category>? categories)
        {
            var distinct = categories?.Distinct().ToList() ?? new List<Category>();
            if (distinct.Count == 0)
                throw new DomainException("A terméknek legalább egy kategóriába tartoznia kell.");

            return distinct;
        }

        public void AddWeeklyStock(DateTime weekStartDate, decimal quantity, Units unit, decimal unitPrice)
        {
            var normalizedWeek = weekStartDate.Date;
            if (_weeklyStocks.Any(s => s.WeekStartDate == normalizedWeek))
                throw new DomainException("Erre a hétre már van rögzített kínálat ehhez a termékhez.");

            _weeklyStocks.Add(ProductStock.Create(this, normalizedWeek, quantity, unit, unitPrice));
        }

        public ProductStock? GetStockForWeek(DateTime weekStartDate)
        {
            var normalizedWeek = weekStartDate.Date;
            return _weeklyStocks.FirstOrDefault(s => s.WeekStartDate == normalizedWeek);
        }
    }
}