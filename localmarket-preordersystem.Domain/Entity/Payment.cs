using localmarket_preordersystem.Domain.Exceptions;
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

        private Payment()
        {
            // EF Core-nak kell
        }

        public static Payment Create(Order order, decimal amount, PaymentMethod method)
        {
            ArgumentNullException.ThrowIfNull(order);
            if (amount <= 0)
                throw new DomainException("A fizetendő összegnek pozitívnak kell lennie.");

            return new Payment
            {
                OrderId = order.Id,
                Order = order,
                Amount = amount,
                Method = method,
                Status = PaymentStatus.Pending,
                Date = DateTime.UtcNow
            };
        }

        public void MarkCompleted()
        {
            if (Status != PaymentStatus.Pending)
                throw new DomainException($"Csak függőben lévő fizetés zárható le, jelenlegi állapot: '{Status}'.");

            Status = PaymentStatus.Completed;
        }

        public void MarkError()
        {
            Status = PaymentStatus.Error;
        }

        public void Refund()
        {
            if (Status != PaymentStatus.Completed)
                throw new DomainException("Csak teljesített fizetés utalható vissza.");

            Status = PaymentStatus.Refunded;
        }

        public void Cancel()
        {
            if (Status is PaymentStatus.Completed or PaymentStatus.Refunded)
                throw new DomainException($"'{Status}' állapotú fizetés nem mondható le.");

            Status = PaymentStatus.Cancelled;
        }
    }
}
