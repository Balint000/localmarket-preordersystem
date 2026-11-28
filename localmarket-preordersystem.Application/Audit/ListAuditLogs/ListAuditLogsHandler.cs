using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Audit.ListAuditLogs;

/// <summary>Az auditnapló lapozott listája (csak piacvezető). A lapozás az adatbázisban történik, a napló mérete így nem számít.</summary>
public sealed class ListAuditLogsHandler(IAuditLogRepository audit, ICurrentUserService currentUser)
{
    public async Task<PagedList<AuditLogDto>> HandleAsync(ListAuditLogsQuery query, CancellationToken ct = default)
    {
        currentUser.RequireRole(Role.Admin);
        new ValidationBuilder()
            .Must(nameof(query.Page), query.Page >= 1, "Az oldalszám legalább 1.")
            .Must(nameof(query.PageSize), query.PageSize is >= 1 and <= 100, "Az oldalméret 1 és 100 között lehet.")
            .ThrowIfInvalid();

        var page = await audit.ListAsync(query.EntityName?.Trim(), query.EntityId, query.Page, query.PageSize, ct);
        return new PagedList<AuditLogDto>(page.Items.Select(AuditLogDto.From).ToList(), page.Total);
    }
}
