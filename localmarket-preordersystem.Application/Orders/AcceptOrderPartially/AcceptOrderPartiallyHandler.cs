using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Orders.AcceptOrderPartially
{
    public sealed record FulfilledLine(int ProductId, decimal Quantity);

    /// <summary>Részleges teljesítés: a felsorolt tételekből ennyit vállal az árus (ami nincs felsorolva, az 0).</summary>
    public sealed record AcceptOrderPartiallyCommand(int OrderId, IReadOnlyList<FulfilledLine> Lines);

    public static class AcceptOrderPartiallyValidator
    {
        public static void Validate(AcceptOrderPartiallyCommand c) => new ValidationBuilder()
            .Must(nameof(c.Lines), c.Lines is not null, "A teljesített tételek megadása kötelező.")
            .Must(nameof(c.Lines), c.Lines is null || c.Lines.All(l => l.Quantity >= 0), "A mennyiség nem lehet negatív.")
            .Must(nameof(c.Lines), c.Lines is null || c.Lines.Select(l => l.ProductId).Distinct().Count() == c.Lines.Count, "Egy termék csak egyszer szerepelhet.")
            .ThrowIfInvalid();
    }

    public sealed class AcceptOrderPartiallyHandler(IOrderRepository orders, IProductRepository products, IAuditLogRepository audit, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<OrderDto> HandleAsync(AcceptOrderPartiallyCommand command, CancellationToken ct = default)
        {
            var order = await OrderSupport.LoadAsync(orders, command.OrderId, ct);
            OrderSupport.RequireProducerOwns(order, currentUser);
            AcceptOrderPartiallyValidator.Validate(command);

            var before = order.Status;
            var released = order.AcceptPartially(command.Lines.ToDictionary(l => l.ProductId, l => l.Quantity));
            // a rendelés az idősávot továbbra is foglalja, csak a hiányzó mennyiség kerül vissza a készletbe
            await OrderSupport.ReleaseAsync(products, order, released, releaseSlot: false, ct);
            await OrderSupport.AuditStatusAsync(audit, currentUser, "OrderPartiallyFulfilled", order, before, ct);
            await uow.SaveChangesAsync(ct);
            return OrderDto.From(order);
        }
    }
}
