using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Orders.ListDisputedOrders
{
    public sealed class ListDisputedOrdersHandler(IOrderRepository orders, ICurrentUserService currentUser)
    {
        public async Task<IReadOnlyList<OrderDto>> HandleAsync(CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Admin);
            return (await orders.ListByStatusAsync(OrderStatus.Disputed, ct)).OrderBy(o => o.CreatedAt).Select(OrderDto.From).ToList();
        }
    }
}
