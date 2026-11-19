using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Validation;

namespace localmarket_preordersystem.Application.Users.UpdateMyProfile
{
    /// <summary>A bejelentkezett felhasználó saját nevének és telefonszámának módosítása (az e-mail és a szerepkör nem módosítható).</summary>
    public sealed class UpdateMyProfileHandler(IUserRepository users, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        public async Task<UserDto> HandleAsync(UpdateMyProfileCommand command, CancellationToken ct = default)
        {
            new ValidationBuilder()
                .Required(nameof(command.FirstName), command.FirstName, "A keresztnév megadása kötelező.")
                .Required(nameof(command.LastName), command.LastName, "A vezetéknév megadása kötelező.")
                .MaxLength(nameof(command.FirstName), command.FirstName, 100)
                .MaxLength(nameof(command.LastName), command.LastName, 100)
                .MaxLength(nameof(command.PhoneNumber), command.PhoneNumber, 30)
                .ThrowIfInvalid();

            var user = await users.GetByIdAsync(currentUser.RequireUserId(), ct)
                ?? throw new NotFoundException("A felhasználó nem található.");

            user.FirstName = command.FirstName.Trim();
            user.LastName = command.LastName.Trim();
            user.PhoneNumber = command.PhoneNumber?.Trim() ?? string.Empty;

            await unitOfWork.SaveChangesAsync(ct);
            return UserDto.From(user);
        }
    }
}
