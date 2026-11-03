using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Cart.UpdateCartItem
{
    public sealed record UpdateCartItemCommand(int ProductId, decimal Quantity);

    public sealed class UpdateCartItemHandler(ICartRepository carts, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<CartDto> HandleAsync(UpdateCartItemCommand command, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Customer);
            var userId = currentUser.RequireUserId();
            new ValidationBuilder()
                .Positive(nameof(command.Quantity), command.Quantity, "A mennyiségnek pozitívnak kell lennie.")
                .ThrowIfInvalid();

            var cart = await carts.GetByUserIdAsync(userId, ct) ?? throw new NotFoundException("A kosár üres.");
            cart.UpdateItemQuantity(command.ProductId, command.Quantity);
            await uow.SaveChangesAsync(ct);
            return CartDto.From(cart);
        }
    }
}
