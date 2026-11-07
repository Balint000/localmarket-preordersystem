using localmarket_preordersystem.Application.Orders.AcceptOrderPartially;

namespace localmarket_preordersystem.api.Contracts.Orders;

public sealed record PartialRequest(IReadOnlyList<FulfilledLine> Lines);
