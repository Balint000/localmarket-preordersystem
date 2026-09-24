using localmarket_preordersystem.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace localmarket_preordersystem.Domain.Entity
{
    public class Market
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        private readonly List<DayOfWeek> _openingDays = new();
        public IReadOnlyCollection<DayOfWeek> OpeningDays => _openingDays.AsReadOnly();

        public List<Producer> Producers { get; private set; } = new();
        public List<PickupSlot> PickupSlots { get; set; } = new();

        private Market()
        {
            // EF Core-nak 
        }

        public static Market Create(string name, string address, IEnumerable<DayOfWeek>? openingDays = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("A piac nevének megadása kötelező.");
            if (string.IsNullOrWhiteSpace(address))
                throw new DomainException("A piac címének megadása kötelező.");

            var market = new Market
            {
                Name = name.Trim(),
                Address = address.Trim(),
                IsActive = true
            };

            if (openingDays is not null)
            {
                foreach (var day in openingDays.Distinct())
                    market._openingDays.Add(day);
            }

            return market;
        }

        public void AddOpeningDay(DayOfWeek day)
            {
                if (!_openingDays.Contains(day))
                    _openingDays.Add(day);
            }

        public void RemoveOpeningDay(DayOfWeek day)
        {
            _openingDays.Remove(day);
        }

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;

    }
}
