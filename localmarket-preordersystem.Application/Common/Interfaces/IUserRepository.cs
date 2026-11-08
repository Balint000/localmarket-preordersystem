using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Common.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
        /// <summary>Felhasználók listája (olvasásra, árus-profillal); a search az e-mailben és a névben keres.</summary>
        Task<IReadOnlyList<User>> ListAsync(Role? role = null, bool? isActive = null, string? search = null, CancellationToken cancellationToken = default);
        Task AddAsync(User user, CancellationToken cancellationToken = default);
    }
}
