using System;
using System.Collections.Generic;
using System.Text;

namespace localmarket_preordersystem.Domain.ValueObject
{
    public enum OrderStatus
    {
        Pending = 1,
        Accepted = 2,
        PartiallyFulfilled = 3,
        PickedUp = 4,
        Cancelled = 5,
        Disputed = 6 // felülvizsgálás alatt; vitás rendeléshez status
    }
}
