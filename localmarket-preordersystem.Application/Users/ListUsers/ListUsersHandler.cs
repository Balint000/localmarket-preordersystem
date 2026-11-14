using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Users.ListUsers
{
    public sealed record ListUsersQuery(Role? Role = null, bool? IsActive = null, string? Search = null);

    /// <summary>Felhasználók listázása (szerepkör / aktivitás / név- vagy e-mail-részlet szerint). Csak piacvezető.</summary>
    public sealed class ListUsersHandler(IUserRepository users, ICurrentUserService currentUser)
    {
        public async Task<IReadOnlyList<UserDto>> HandleAsync(ListUsersQuery query, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Admin);

            var list = await users.ListAsync(query.Role, query.IsActive, query.Search?.Trim(), ct);
            return list.Select(UserDto.From).ToList();
        }
    }
}
