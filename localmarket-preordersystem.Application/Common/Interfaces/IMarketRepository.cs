using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Common.Interfaces
{
    public interface IMarketRepository
    {
        Task<Market?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Market>> ListAsync(CancellationToken cancellationToken = default);
        Task AddAsync(Market market, CancellationToken cancellationToken = default);
    }
}
