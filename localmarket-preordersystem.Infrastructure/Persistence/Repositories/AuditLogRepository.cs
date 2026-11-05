using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace localmarket_preordersystem.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogRepository(MarketDbContext context) : IAuditLogRepository
{
    public async Task AddAsync(Logs log, CancellationToken cancellationToken = default)
        => await context.AuditLogs.AddAsync(log, cancellationToken);

    public async Task<IReadOnlyList<Logs>> ListAsync(string? entityName = null, int? entityId = null, CancellationToken cancellationToken = default)
    {
        var query = context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName)) query = query.Where(l => l.EntityName == entityName);
        if (entityId.HasValue) query = query.Where(l => l.EntityId == entityId.Value);

        return await query.OrderByDescending(l => l.CreatedAt).ToListAsync(cancellationToken);
    }
}