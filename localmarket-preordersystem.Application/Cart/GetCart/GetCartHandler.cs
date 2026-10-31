using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Cart.GetCart
{
    public sealed class GetCartHandler(ICartRepository carts, ICurrentUserService currentUser)
    {
        public async Task<CartDto> HandleAsync(CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Customer);
            var cart = await carts.GetByUserIdAsync(currentUser.RequireUserId(), ct);
            return cart is null ? CartDto.Empty : CartDto.From(cart);
        }
    }
}
