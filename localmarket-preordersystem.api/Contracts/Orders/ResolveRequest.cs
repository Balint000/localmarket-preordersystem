using localmarket_preordersystem.Application.Orders.ResolveDispute;

namespace localmarket_preordersystem.api.Contracts.Orders;

public sealed record ResolveRequest(DisputeDecision Decision, string? Note);
