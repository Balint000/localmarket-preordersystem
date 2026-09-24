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

        /// <summary>
        /// Coordinates of the market.
        /// Ai suggested that to use these when we connect to the weather API.
        /// </summary>
        public double? Latitude { get; private set; }
        public double? Longitude { get; private set; }

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

        public void UpdateLocation(double? latitude, double? longitude)
        {
            if (latitude.HasValue != longitude.HasValue)
                throw new DomainException("A szélességi és hosszúsági koordinátát együtt kell megadni.");
            if (latitude is < -90 or > 90)
                throw new DomainException("A szélességi koordináta -90 és 90 között lehet.");
            if (longitude is < -180 or > 180)
                throw new DomainException("A hosszúsági koordináta -180 és 180 között lehet.");

            Latitude = latitude;
            Longitude = longitude;
        }

        public void AddOpeningDay(DayOfWeek day)
        {
            if (!_openingDays.Contains(day)) _openingDays.Add(day);
        }

        public void RemoveOpeningDay(DayOfWeek day) => _openingDays.Remove(day);

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;

    }
}
