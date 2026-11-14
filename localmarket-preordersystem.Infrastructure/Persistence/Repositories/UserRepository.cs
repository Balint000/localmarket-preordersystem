using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;
using localmarket_preordersystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace localmarket_preordersystem.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(MarketDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => context.Users.Include(u => u.Producer)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    // A User.Register az e-mailt mindig kisbetűsre normalizálja, ezért itt elég az egyenlőség-vizsgálat;
    // a korábbi `Email.ToLower() == ...` nem tudta használni az Email egyedi indexét (teljes táblaolvasás).
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return context.Users.Include(u => u.Producer)
            .FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return context.Users.AnyAsync(u => u.Email == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListAsync(Role? role = null, bool? isActive = null, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = context.Users.AsNoTracking().Include(u => u.Producer).AsQueryable();

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{EscapeLike(search.Trim())}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Email, pattern)
                || EF.Functions.ILike(u.FirstName, pattern)
                || EF.Functions.ILike(u.LastName, pattern)
                || EF.Functions.ILike(u.LastName + " " + u.FirstName, pattern));
        }

        return await query
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ThenBy(u => u.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        => await context.Users.AddAsync(user, cancellationToken);

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
