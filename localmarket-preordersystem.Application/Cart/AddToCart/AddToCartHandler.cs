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
            var stock = product.GetNextAvailableStock(time.GetLocalNow().Date)
                ?? throw new ConflictException($"A(z) '{product.Name}' termékhez jelenleg nincs meghirdetett kínálat.");

            var cart = await CartSupport.GetOrCreateAsync(carts, users, userId, ct);
            cart.AddItem(product, command.Quantity, stock.UnitPrice);

            await uow.SaveChangesAsync(ct);
            return CartDto.From(cart);
        }
    }
}
