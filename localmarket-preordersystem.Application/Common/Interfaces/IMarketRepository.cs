using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Common.Interfaces
{
    public interface IMarketRepository
    {
        Task<Market?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    }
}