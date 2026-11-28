using Microsoft.Extensions.Logging;
using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Orders.PlaceOrder
{
    public sealed record PickupChoice(int ProducerId, int PickupSlotId);

    /// <summary>Checkout: a kosár minden árusához meg kell adni egy átvételi idősávot.</summary>
    public sealed record PlaceOrderCommand(PaymentMethod PaymentMethod, IReadOnlyList<PickupChoice> Pickups);

    public static class PlaceOrderValidator
    {
        public static void Validate(PlaceOrderCommand c) => new ValidationBuilder()
            .Must(nameof(c.PaymentMethod), Enum.IsDefined(c.PaymentMethod), "Érvénytelen fizetési mód.")
            .Must(nameof(c.Pickups), c.Pickups is { Count: > 0 }, "Minden árushoz meg kell adni az átvételi időpontot.")
            .Must(nameof(c.Pickups), c.Pickups is null || c.Pickups.All(p => p is not null), "Érvénytelen átvételi tétel.")
            .Must(nameof(c.Pickups), c.Pickups is null || c.Pickups.Any(p => p is null) || c.Pickups.Select(p => p.ProducerId).Distinct().Count() == c.Pickups.Count, "Árusonként csak egy átvételi időpont adható meg.")
            .ThrowIfInvalid();
    }

    /// <summary>
    /// A többárusos kosárból árusonként egy rendelés lesz. Készlet- és idősáv-foglalás a leadáskor,
    /// az egész egyetlen mentés (mindent vagy semmit): bármelyik árusnál elbukó szabály az egészet visszautasítja.
    /// </summary>
    public sealed class PlaceOrderHandler(
        ICartRepository carts,
        IUserRepository users,
        IProductRepository products,
        IPickupSlotRepository slots,
        IOrderRepository orders,
        IAuditLogRepository audit,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        TimeProvider time,
        ILogger<PlaceOrderHandler> logger)
    {
        public async Task<IReadOnlyList<OrderDto>> HandleAsync(PlaceOrderCommand command, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Customer);
            var userId = currentUser.RequireUserId();
            PlaceOrderValidator.Validate(command);

            var cart = await carts.GetByUserIdAsync(userId, ct);
            if (cart is null || cart.Items.Count == 0)
                throw new ConflictException("A kosár üres.");

            var customer = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("A felhasználó nem található.");
            var now = time.GetLocalNow().DateTime;

            // friss, követett termékek (készlet-módosításhoz), nem a kosárban lévő pillanatképek
            var loaded = (await products.GetByIdsAsync(cart.Items.Select(i => i.ProductId).Distinct(), ct)).ToDictionary(p => p.Id);

            var cartProducerIds = cart.Items.Select(i => i.Product.ProducerId).Distinct().ToList();
            var missing = cartProducerIds.Except(command.Pickups.Select(p => p.ProducerId)).ToList();
            if (missing.Count > 0)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    [nameof(command.Pickups)] = new[] { $"Hiányzik az átvételi időpont a következő árus(ok)hoz: {string.Join(", ", missing)}." }
                });
            var extra = command.Pickups.Select(p => p.ProducerId).Except(cartProducerIds).ToList();
            if (extra.Count > 0)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    [nameof(command.Pickups)] = new[] { "Olyan árushoz is megadtál időpontot, akitől nincs termék a kosárban." }
                });

            var placed = new List<Order>();
            foreach (var group in cart.Items.GroupBy(i => i.Product.ProducerId))
            {
                var choice = command.Pickups.Single(p => p.ProducerId == group.Key);
                // az árus és a piac is betöltődik: foglaláskor újraellenőrizzük a nyitvatartást és a piac állapotát
                var slot = await slots.GetForUpdateAsync(choice.PickupSlotId, ct)
                    ?? throw new NotFoundException($"Nem található átvételi időpont: {choice.PickupSlotId}.");
                if (slot.ProducerId != group.Key)
                    throw new ValidationException(new Dictionary<string, string[]>
                    {
                        [nameof(command.Pickups)] = new[] { $"A(z) {slot.Id} időpont nem ehhez az árushoz tartozik." }
                    });

                var lines = new List<(Product Product, decimal Quantity)>();
                foreach (var i in group)
                {
                    if (!loaded.TryGetValue(i.ProductId, out var prod))
                        throw new ConflictException("A kosárban lévő termék már nem elérhető, töltsd újra a kosarat.");
                    lines.Add((prod, i.Quantity));
                }

                var order = Order.Place(customer, slot, lines, now);
                order.SetPayment(command.PaymentMethod);
                placed.Add(order);
            }

            foreach (var order in placed)
                await orders.AddAsync(order, ct);
            cart.Clear();
            await uow.SaveChangesAsync(ct);

            // naplózás a mentés után (az Id-k ekkor már megvannak)
            try
            {
                foreach (var order in placed)
                    await audit.RecordAsync(currentUser, "OrderPlaced", "Order", order.Id, null, new { order.Status, order.ProducerId, order.TotalAmount }, ct);
                await uow.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // a rendelés már mentve van, az audit hibája nem buktathatja a kérést
                logger.LogError(ex, "OrderPlaced audit mentése sikertelen: {Ids}", string.Join(",", placed.Select(o => o.Id)));
            }

            return placed.Select(OrderDto.From).ToList();
        }
    }
}
