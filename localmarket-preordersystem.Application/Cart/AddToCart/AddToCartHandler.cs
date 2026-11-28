using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Cart.AddToCart
{
    public sealed record AddToCartCommand(int ProductId, decimal Quantity);

    public static class AddToCartValidator
    {
        public static void Validate(AddToCartCommand c) => new ValidationBuilder()
            .Positive(nameof(c.ProductId), c.ProductId, "Érvénytelen termék azonosító.")
            .Positive(nameof(c.Quantity), c.Quantity, "A mennyiségnek pozitívnak kell lennie.")
            .ThrowIfInvalid();
    }

    /// <summary>Termék kosárba tétele. Csak vevő. Az ár a legközelebbi meghirdetett hét ára (tájékoztató pillanatkép).</summary>
    public sealed class AddToCartHandler(ICartRepository carts, IUserRepository users, IProductRepository products, IUnitOfWork uow, ICurrentUserService currentUser, TimeProvider time)
    {
        public async Task<CartDto> HandleAsync(AddToCartCommand command, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Customer);
            var userId = currentUser.RequireUserId();
            AddToCartValidator.Validate(command);

            var product = await products.GetByIdAsync(command.ProductId, ct)
                ?? throw new NotFoundException($"Nem található termék: {command.ProductId}.");
            if (product.IsArchived)
                throw new ConflictException($"A(z) '{product.Name}' termék már nem rendelhető.");
            var now = time.GetLocalNow().DateTime;
            // a hét utolsó piacnapja után az aktuális heti kínálat már nem számít: a következő meghirdetett hét jön
            var stock = product.GetNextAvailableStock(now)
                ?? throw new ConflictException($"A(z) '{product.Name}' termékhez jelenleg nincs rendelhető kínálat (erre a hétre már nincs piacnap, a következő hétre még nincs meghirdetve).");

            var inCart = (await carts.GetByUserIdAsync(userId, ct))?.Items.FirstOrDefault(i => i.ProductId == product.Id)?.Quantity ?? 0m;
            CartSupport.ValidateQuantity(stock.Unit, command.Quantity);
            CartSupport.ValidateQuantity(stock.Unit, inCart + command.Quantity);

            var cart = await CartSupport.GetOrCreateAsync(carts, users, userId, ct);
            cart.AddItem(product, command.Quantity, stock.UnitPrice);

            await uow.SaveChangesAsync(ct);
            return CartDto.From(cart, now);
        }
    }
}
