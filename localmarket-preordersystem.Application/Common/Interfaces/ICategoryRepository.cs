using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Common.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default);
    /// <summary>Követett entitás (módosításhoz / törléshez).</summary>
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    /// <summary>Kis-nagybetűtől független névütközés; az excludeId azonosítójú kategóriát kihagyja (átnevezéshez).</summary>
    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    /// <summary>Tartozik-e a kategóriához legalább egy termék.</summary>
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    void Remove(Category category);
}
