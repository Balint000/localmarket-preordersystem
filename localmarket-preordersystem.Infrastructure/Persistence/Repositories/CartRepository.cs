using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace localmarket_preordersystem.Infrastructure.Persistence.Repositories;

internal sealed class CartRepository(MarketDbContext context) : ICartRepository
{
    public Task<Cart?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        => context.Carts
            .Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public async Task AddAsync(Cart cart, CancellationToken cancellationToken = default)
        => await context.Carts.AddAsync(cart, cancellationToken);

    public async Task RemoveItemsForProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        var items = await context.CartItems.Where(i => i.ProductId == productId).ToListAsync(cancellationToken);
        context.CartItems.RemoveRange(items);
    }
}