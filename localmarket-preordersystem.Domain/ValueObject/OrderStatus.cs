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
        Disputed = 6, // felülvizsgálás alatt; vitás rendeléshez status
        Rejected = 7, // árus elutasította

        Ready = 8, // összekészítve, átvehető
        NotPickedUp = 9, // az idősáv lejárt, a vevő nem vette át (az árus jelöli)
        Expired = 10, // az árus az idősáv végéig nem válaszolt a függő rendelésre (automatikus lezárás)
    }
}
