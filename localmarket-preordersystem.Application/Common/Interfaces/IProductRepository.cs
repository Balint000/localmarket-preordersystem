using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Common.Interfaces;

public interface IProductRepository
{
    /// <summary>Betöltve: Categories, Allergies, WeeklyStocks (a készlet-műveletekhez szükséges).</summary>
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> SearchAsync(ProductSearchCriteria criteria, CancellationToken cancellationToken = default);
    /// <summary>Szerepel-e a termék bármelyik (akár lemondott) rendelés tételei között.</summary>
    Task<bool> HasOrdersAsync(int productId, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    void Remove(Product product);
}
