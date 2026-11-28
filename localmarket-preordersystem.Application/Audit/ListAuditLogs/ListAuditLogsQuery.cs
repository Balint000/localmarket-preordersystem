namespace localmarket_preordersystem.Application.Audit.ListAuditLogs;

public sealed record ListAuditLogsQuery(string? EntityName, int? EntityId, int Page = 1, int PageSize = 25);
