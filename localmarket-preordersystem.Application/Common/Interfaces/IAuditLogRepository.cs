using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Common.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(Logs log, CancellationToken cancellationToken = default);
    /// <summary>A napló egy oldala (legújabb elöl), adatbázis-szinten lapozva.</summary>
    Task<PagedList<Logs>> ListAsync(string? entityName, int? entityId, int page, int pageSize, CancellationToken cancellationToken = default);
}