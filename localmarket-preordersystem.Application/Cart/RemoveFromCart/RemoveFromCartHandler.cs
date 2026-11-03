using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Cart.RemoveFromCart
{
    public sealed record RemoveFromCartCommand(int ProductId);

    public sealed class RemoveFromCartHandler(ICartRepository carts, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<CartDto> HandleAsync(RemoveFromCartCommand command, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Customer);
            var userId = currentUser.RequireUserId();
            var cart = await carts.GetByUserIdAsync(userId, ct) ?? throw new NotFoundException("A kosár üres.");
            cart.RemoveItem(command.ProductId);
            await uow.SaveChangesAsync(ct);
            return CartDto.From(cart);
        }
    }
}
