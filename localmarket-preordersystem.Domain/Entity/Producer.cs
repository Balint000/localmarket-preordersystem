using localmarket_preordersystem.Domain.Exceptions;
using localmarket_preordersystem.Domain.ValueObject;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace localmarket_preordersystem.Domain.Entity
{
    public class Producer
    {
        public int Id { get; private set; }
        public string Name { get; private set; } = string.Empty;

        public int MarketId { get; set; }
        public Market Market { get; set; } = null!;

        public int StallNumber { get; private set; } = 0;

        public ProducerStatus Status { get; private set; } = ProducerStatus.Pending;

        public List<Product> Products { get; set; } = new();

        public List<PickupSlot> PickupSlots { get; private set; } = new();

        public string? RejectionReason { get; private set; } // elutasítás indoklásának tárolása
        public DateTime RegisteredAt { get; private set; } = DateTime.UtcNow; // mikor lett elfogadva

        private Producer()
        {
            // EF Core-nak
        }

        public static Producer Register(Market market, string name, int stallNumber)
        {
            ArgumentNullException.ThrowIfNull(market);
            if (!market.IsActive)
                throw new DomainException("Inaktív piacra nem lehet árusként regisztrálni.");
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Az árus nevének megadása kötelező.");
            if (stallNumber < 0)
                throw new DomainException("A standszám nem lehet negatív.");

            return new Producer
            {
                MarketId = market.Id,
                Market = market,
                Name = name.Trim(),
                StallNumber = stallNumber,
                Status = ProducerStatus.Pending,
                RegisteredAt = DateTime.UtcNow,
            };
        }

        /// <summary>Piacvezető jóváhagyja az árust, függőben lévő státuszból lehetséges.</summary>
        public void Approve()
        {
            if (Status != ProducerStatus.Pending)
                throw new DomainException($"Csak függőben lévő árus hagyható jóvá, jelenlegi státusz: '{Status}'.");

            Status = ProducerStatus.Approved;
        }

        public void Reject(string reason)
        {
            if (Status != ProducerStatus.Pending)
                throw new DomainException($"Csak függőben lévő árus utasítható el, jelenlegi státusz: '{Status}'.");
            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainException("Elutasításhoz indoklás megadása kötelező.");

            Status = ProducerStatus.Rejected;
            RejectionReason = reason;
        }

        /// <summary>A Product.Create ezt hívja meg, csak jóváhagyott árus listázhat terméket.</summary>
        public bool CanListProducts() => Status == ProducerStatus.Approved;

        public void Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Az árus nevének megadása kötelező.");
            Name = name.Trim();
        }

        /// <summary>
        /// Fix stand kiosztása (piacvezető). 0 = nincs kiosztva. Hogy a stand létezik-e a piac elrendezésében,
        /// és nem foglalt-e, az Application ellenőrzi (más árusok adatára van szükség).
        /// </summary>
        public void AssignStall(int stallNumber)
        {
            if (stallNumber < 0)
                throw new DomainException("A standszám nem lehet negatív.");
            if (stallNumber > 0 && Market is { } market && market.Layout.Stalls.Count > 0 && !market.Layout.HasStall(stallNumber))
                throw new DomainException($"A(z) {stallNumber}. stand nem szerepel a piac elrendezésében.");
            StallNumber = stallNumber;
        }

        public void UnassignStall() => StallNumber = 0;

        public bool HasStall => StallNumber > 0;
    }
}
