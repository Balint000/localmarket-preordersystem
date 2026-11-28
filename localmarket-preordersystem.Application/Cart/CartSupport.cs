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
        /// <summary>A mennyiség a mértékegység rendelési léptékének többszöröse kell legyen (kg/l: 0,5-ösével stb.).</summary>
        public static void ValidateQuantity(Units unit, decimal quantity)
        {
            var step = unit.OrderStep();
            new ValidationBuilder()
                .Must("Quantity", unit.IsValidOrderQuantity(quantity),
                    $"Ennél a terméknél a mennyiség {step:0.###}-es lépésekben adható meg (pl. {step:0.###}, {step * 2:0.###}, {step * 3:0.###}).")
                .ThrowIfInvalid();
        }

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
