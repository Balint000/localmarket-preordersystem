using CartEntity = localmarket_preordersystem.Domain.Entity.Cart;

namespace localmarket_preordersystem.Application.Common.Interfaces;

public interface ICartRepository
{
    /// <summary>Betöltve: Items, azok Product-ja (és annak Producer-e).</summary>
    Task<CartEntity?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task AddAsync(CartEntity cart, CancellationToken cancellationToken = default);
}
