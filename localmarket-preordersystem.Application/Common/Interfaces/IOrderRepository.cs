using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Common.Interfaces;

public interface IOrderRepository
{
    /// <summary>Betöltve: Items, PickupSlot, Payment (és a tételek Product-ja a készlet-felszabadításhoz).</summary>
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> ListByUserAsync(int userId, OrderStatus? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> ListByProducerAsync(int producerId, OrderStatus? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> ListByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
    /// <summary>Az árus aktív (nem lemondott/elutasított) rendelései egy átvételi napra — csomagolólista.</summary>
    /// <summary>Egy átvételi nap rendelései (producerId nélkül: a piac összes árusáé), a lemondott/elutasított/lejárt nélkül.</summary>
    Task<IReadOnlyList<Order>> ListForPickingAsync(int? producerId, DateTime pickupDate, CancellationToken cancellationToken = default);
    /// <summary>Hivatkozik-e az idősávra bármelyik (akár lemondott) rendelés.</summary>
    /// <summary>Függő rendelések, amelyek idősávja a megadott (helyi) időpontig véget ért — módosításra betöltve.</summary>
    Task<IReadOnlyList<Order>> ListPendingWithEndedSlotAsync(DateTime localNow, int take, CancellationToken cancellationToken = default);
    /// <summary>Az adott terméket tartalmazó aktív (függő, elfogadott, részben elfogadott, átvehető) rendelések, módosításra.</summary>
    Task<IReadOnlyList<Order>> ListActiveContainingProductAsync(int productId, CancellationToken cancellationToken = default);
    Task<bool> AnyForPickupSlotAsync(int pickupSlotId, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
}
