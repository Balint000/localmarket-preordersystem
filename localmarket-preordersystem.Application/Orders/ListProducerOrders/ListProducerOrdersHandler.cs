using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Orders.ListProducerOrders
{
    public sealed record ListProducerOrdersQuery(OrderStatus? Status = null);

    public sealed class ListProducerOrdersHandler(IOrderRepository orders, ICurrentUserService currentUser)
    {
        public async Task<IReadOnlyList<OrderDto>> HandleAsync(ListProducerOrdersQuery query, CancellationToken ct = default)
        {
            var producerId = currentUser.RequireProducerId();
            return (await orders.ListByProducerAsync(producerId, query.Status, ct))
                .OrderByDescending(o => o.CreatedAt).Select(OrderDto.From).ToList();
        }
    }
}
