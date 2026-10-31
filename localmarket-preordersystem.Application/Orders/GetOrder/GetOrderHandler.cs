using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Orders.GetOrder
{
    public sealed class GetOrderHandler(IOrderRepository orders, ICurrentUserService currentUser)
    {
        public async Task<OrderDto> HandleAsync(int orderId, CancellationToken ct = default)
        {
            var order = await OrderSupport.LoadAsync(orders, orderId, ct);
            OrderSupport.RequireCanView(order, currentUser);
            return OrderDto.From(order);
        }
    }
}
