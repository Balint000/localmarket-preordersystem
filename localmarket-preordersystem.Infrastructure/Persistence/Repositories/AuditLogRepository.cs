using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace localmarket_preordersystem.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogRepository(MarketDbContext context) : IAuditLogRepository
{
    public async Task AddAsync(Logs log, CancellationToken cancellationToken = default)
        => await context.AuditLogs.AddAsync(log, cancellationToken);

    public async Task<PagedList<Logs>> ListAsync(string? entityName, int? entityId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityName)) query = query.Where(l => l.EntityName == entityName);
        if (entityId.HasValue) query = query.Where(l => l.EntityId == entityId.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedList<Logs>(items, total);
    }
}
