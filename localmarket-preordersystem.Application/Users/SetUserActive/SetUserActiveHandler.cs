using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Users.SetUserActive
{
    public sealed record SetUserActiveCommand(int UserId, bool IsActive);

    /// <summary>
    /// Felhasználói fiók letiltása / újraengedélyezése (csak piacvezető). A letiltás azonnal hat: az API
    /// minden kérésnél ellenőrzi a fiók állapotát, így a még érvényes token is elutasításra kerül.
    /// Saját fiók és az utolsó aktív piacvezető nem tiltható le.
    /// </summary>
    public sealed class SetUserActiveHandler(IUserRepository users, IAuditLogRepository audit, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<UserDto> HandleAsync(SetUserActiveCommand command, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Admin);
            var myId = currentUser.RequireUserId();

            var user = await users.GetByIdAsync(command.UserId, ct)
                ?? throw new NotFoundException($"Nem található felhasználó: {command.UserId}.");

            if (user.IsActive == command.IsActive)
                return UserDto.From(user);

            if (!command.IsActive)
            {
                if (user.Id == myId)
                    throw new ConflictException("A saját fiókodat nem tilthatod le.");
                if (user.Role == Role.Admin && await users.CountActiveAdminsAsync(ct) <= 1)
                    throw new ConflictException("Az utolsó aktív piacvezető nem tiltható le.");
                user.Deactivate();
            }
            else
            {
                user.Activate();
            }

            await audit.RecordAsync(currentUser, command.IsActive ? "UserActivated" : "UserDeactivated", "User", user.Id,
                new { IsActive = !command.IsActive }, new { IsActive = command.IsActive }, ct);
            await uow.SaveChangesAsync(ct);
            return UserDto.From(user);
        }
    }
}
