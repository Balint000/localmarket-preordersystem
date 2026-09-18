using localmarket_preordersystem.Domain.Exceptions;
using localmarket_preordersystem.Domain.ValueObject;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace localmarket_preordersystem.Domain.Entity
{
    public class Product
    {
        public int Id { get; private set; }


        public int ProducerId { get; private set; }
        public Producer Producer { get; private set; } = null!;

        //[Required]
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        private readonly List<Category> _categories = new();
        public IReadOnlyCollection<Category> Categories => _categories.AsReadOnly();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        private readonly List<Allergy> _allergies = new();
        public IReadOnlyCollection<Allergy> Allergies => _allergies.AsReadOnly();

        private readonly List<ProductStock> _weeklyStocks = new();
        public IReadOnlyCollection<ProductStock> WeeklyStocks => _weeklyStocks.AsReadOnly();

        private Product()
        {
            // EF Core-nak 
        }

        public static Product Create(Producer producer, string name, string description, IEnumerable<Category> categories, IEnumerable<Allergy>? allergies = null)
        {
            ArgumentNullException.ThrowIfNull(producer);
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("A termék nevének megadása kötelező.");

            var product = new Product
            {
                ProducerId = producer.Id,
                Producer = producer,
                Name = name.Trim(),
                Description = description?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            if (allergies is not null)
                product._allergies.AddRange(allergies.Distinct());

            if (categories is not null)
                product._categories.AddRange(categories.Distinct());


            return product;
        }

        public void UpdateDetails(string name, string description, IEnumerable<Category> categories, IEnumerable<Allergy> allergies)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("A termék nevének megadása kötelező.");

            Name = name.Trim();
            Description = description?.Trim() ?? string.Empty;

            _categories.Clear();
            _categories.AddRange(categories.Distinct());

            _allergies.Clear();
            _allergies.AddRange(allergies.Distinct());
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
