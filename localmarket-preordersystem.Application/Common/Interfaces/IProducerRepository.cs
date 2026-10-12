using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Common.Interfaces
{
    public interface IProducerRepository
    {
        Task<Producer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Producer>> ListAsync(ProducerStatus? status = null, int? marketId = null, CancellationToken cancellationToken = default);
        Task AddAsync(Producer producer, CancellationToken cancellationToken = default);
    }
}