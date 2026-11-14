using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Audit.ListAuditLogs;

public sealed record AuditLogDto(
    int Id, int? UserId, string Action, string EntityName, int EntityId,
    string? OldValue, string? NewValue, DateTime CreatedAt)
{
    public static AuditLogDto From(Logs l)
        => new(l.Id, l.UserId, l.Action, l.EntityName, l.EntityId, l.OldValue, l.NewValue, l.CreatedAt);
}
