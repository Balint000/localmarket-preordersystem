using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Common.Interfaces
{
    public interface IProducerRepository
    {
        Task<Producer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task AddAsync(Producer producer, CancellationToken cancellationToken = default);
    }
}