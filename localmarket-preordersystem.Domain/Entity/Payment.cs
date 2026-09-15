using localmarket_preordersystem.Domain.ValueObject;
using System;
using System.Collections.Generic;
using System.Text;

namespace localmarket_preordersystem.Domain.Entity
{
    public class Payment
    {
        public int Id { get; private set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public decimal Amount{ get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public PaymentMethod Method { get; set; } = PaymentMethod.DishWashing;
        public DateTime Date { get; set; }
    }
}
