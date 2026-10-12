using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Common.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(Logs log, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Logs>> ListAsync(string? entityName = null, int? entityId = null, CancellationToken cancellationToken = default);
}