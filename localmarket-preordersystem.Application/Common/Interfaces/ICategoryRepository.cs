using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Common.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
}