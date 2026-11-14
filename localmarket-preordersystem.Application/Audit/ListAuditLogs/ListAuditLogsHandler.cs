using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Audit.ListAuditLogs;

public sealed class ListAuditLogsHandler(IAuditLogRepository audit, ICurrentUserService currentUser)
{
    public async Task<IReadOnlyList<AuditLogDto>> HandleAsync(ListAuditLogsQuery query, CancellationToken ct = default)
    {
        currentUser.RequireRole(Role.Admin);
        var logs = await audit.ListAsync(query.EntityName?.Trim(), query.EntityId, ct);
        return logs.Select(AuditLogDto.From).ToList();
    }
}
