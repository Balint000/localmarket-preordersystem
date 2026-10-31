using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Orders.ListMyOrders
{
    public sealed record ListMyOrdersQuery(OrderStatus? Status = null);

    public sealed class ListMyOrdersHandler(IOrderRepository orders, ICurrentUserService currentUser)
    {
        public async Task<IReadOnlyList<OrderDto>> HandleAsync(ListMyOrdersQuery query, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Customer);
            return (await orders.ListByUserAsync(currentUser.RequireUserId(), query.Status, ct))
                .OrderByDescending(o => o.CreatedAt).Select(OrderDto.From).ToList();
        }
    }
}
