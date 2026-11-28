using localmarket_preordersystem.Domain.Exceptions;
using localmarket_preordersystem.Domain.ValueObject;
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

        /// <summary>A piac koordinátái (az időjárás-előrejelzéshez; opcionális, hiányában a címből geokódolunk).</summary>
        public double? Latitude { get; private set; }
        public double? Longitude { get; private set; }

        /// <summary>Nyitvatartás a piacnapokon (helyi idő). Az átvételi idősávoknak ezen belül kell lenniük.</summary>
        public TimeOnly OpensAt { get; private set; } = DefaultOpensAt;
        public TimeOnly ClosesAt { get; private set; } = DefaultClosesAt;

        public static readonly TimeOnly DefaultOpensAt = new(7, 0);
        public static readonly TimeOnly DefaultClosesAt = new(13, 0);

        /// <summary>A piac felülnézeti elrendezése (standok a rácson). Új piacnál üres 12 × 8-as rács.</summary>
        public MarketLayout Layout { get; private set; } = MarketLayout.Empty;

        private readonly List<DayOfWeek> _openingDays = new();
        public IReadOnlyCollection<DayOfWeek> OpeningDays => _openingDays.AsReadOnly();

        public List<Producer> Producers { get; private set; } = new();

        private Market()
        {
            // EF Core-nak
        }

        public static Market Create(string name, string address, IEnumerable<DayOfWeek>? openingDays = null, TimeOnly? opensAt = null, TimeOnly? closesAt = null)
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

            market.SetOpeningHours(opensAt ?? DefaultOpensAt, closesAt ?? DefaultClosesAt);
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

        public void UpdateDetails(string name, string address)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("A piac nevének megadása kötelező.");
            if (string.IsNullOrWhiteSpace(address))
                throw new DomainException("A piac címének megadása kötelező.");
            Name = name.Trim();
            Address = address.Trim();
        }

        public void SetOpeningDays(IEnumerable<DayOfWeek> days)
        {
            ArgumentNullException.ThrowIfNull(days);
            var distinct = days.Distinct().ToList();
            _openingDays.Clear();
            _openingDays.AddRange(distinct);
        }

        public void SetOpeningHours(TimeOnly opensAt, TimeOnly closesAt)
        {
            if (closesAt <= opensAt)
                throw new DomainException("A zárás időpontja a nyitás után kell legyen.");
            OpensAt = opensAt;
            ClosesAt = closesAt;
        }

        /// <summary>Aktív-e a piac, és nyitva tart-e a megadott napon.</summary>
        public bool IsOpenOn(DateTime date) => IsActive && _openingDays.Contains(date.DayOfWeek);

        /// <summary>A megadott időablak a nyitvatartáson belül van-e.</summary>
        public bool CoversTime(TimeOnly start, TimeOnly end) => start >= OpensAt && end <= ClosesAt;

        /// <summary>
        /// Van-e még nyitvatartás a <paramref name="now"/>-t tartalmazó héten (ma zárás előtt, vagy a hét egy
        /// későbbi piacnapján). Ha nincs, az adott heti kínálat már nem rendelhető.
        /// </summary>
        public bool HasOpeningLeftInWeek(DateTime now)
        {
            if (!IsActive) return false;
            var weekEnd = ProductStock.GetWeekStart(now).AddDays(7);
            for (var day = now.Date; day < weekEnd; day = day.AddDays(1))
            {
                if (!_openingDays.Contains(day.DayOfWeek)) continue;
                if (day > now.Date || now.TimeOfDay < ClosesAt.ToTimeSpan()) return true;
            }
            return false;
        }

        /// <summary>
        /// Új elrendezés. Hogy a kiosztott standok közül melyik szűnik meg (és az árus standja ezzel törlődik),
        /// azt az Application dönti el, mert az árusok egy másik aggregate-hez tartoznak.
        /// </summary>
        public void SetLayout(MarketLayout layout)
        {
            ArgumentNullException.ThrowIfNull(layout);
            Layout = layout;
        }

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;

    }
}
