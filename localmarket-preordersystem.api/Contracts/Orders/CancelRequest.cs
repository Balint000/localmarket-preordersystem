namespace localmarket_preordersystem.api.Contracts.Orders;

/// <summary>Lemondás indoklása: árusnál és piacvezetőnél kötelező, vevőnél elhagyható (a törzs is elhagyható).</summary>
public sealed record CancelRequest(string? Reason);
