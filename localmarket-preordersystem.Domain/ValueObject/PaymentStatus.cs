using System;
using System.Collections.Generic;
using System.Text;

namespace localmarket_preordersystem.Domain.ValueObject
{
    public enum PaymentStatus
    {
        Pending = 1,
        Completed = 2,
        Cancelled = 3,
        Error = 4,
        Refunded = 5 // cancel után, ha előre utalta
    }
}
