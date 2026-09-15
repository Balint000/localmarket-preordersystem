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

        public List<DayOfWeek> OpeningDays { get; set; } = new();
        public List<Producer> Producers { get; set; } = new();
        public List<PickupSlot> PickupSlots { get; set; } = new();
    }
}
