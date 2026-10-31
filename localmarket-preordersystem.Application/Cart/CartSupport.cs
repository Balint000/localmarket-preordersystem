using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Cart
{
    internal static class CartSupport
    {
        public static async Task<Domain.Entity.Cart> GetOrCreateAsync(ICartRepository carts, IUserRepository users, int userId, CancellationToken ct)
        {
            var cart = await carts.GetByUserIdAsync(userId, ct);
            if (cart is not null) return cart;

            var user = await users.GetByIdAsync(userId, ct)
                ?? throw new NotFoundException("A felhasználó nem található.");
            cart = Domain.Entity.Cart.CreateEmpty(user);
            await carts.AddAsync(cart, ct);
            return cart;
        }
    }
}
