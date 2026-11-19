using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Validation;

namespace localmarket_preordersystem.Application.Users.ChangeMyEmail
{
    /// <summary>
    /// E-mail cím módosítása DEMÓ MÓDBAN: nincs e-mailes megerősítés, a változás azonnal érvénybe lép.
    /// Biztonsági okból a jelenlegi jelszót kéri. Éles környezetben megerősítő levelet kellene küldeni.
    /// </summary>
    public sealed class ChangeMyEmailHandler(IUserRepository users, IPasswordHasher hasher, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        public async Task<UserDto> HandleAsync(ChangeMyEmailCommand command, CancellationToken ct = default)
        {
            new ValidationBuilder()
                .Required(nameof(command.CurrentPassword), command.CurrentPassword, "A jelenlegi jelszó megadása kötelező.")
                .Email(nameof(command.NewEmail), command.NewEmail)
                .MaxLength(nameof(command.NewEmail), command.NewEmail, 320)
                .ThrowIfInvalid();

            var user = await users.GetByIdAsync(currentUser.RequireUserId(), ct)
                ?? throw new NotFoundException("A felhasználó nem található.");

            if (!hasher.Verify(command.CurrentPassword, user.PasswordHash))
                new ValidationBuilder().Add(nameof(command.CurrentPassword), "A jelenlegi jelszó hibás.").ThrowIfInvalid();

            var normalized = command.NewEmail.Trim().ToLowerInvariant();
            if (normalized != user.Email)
            {
                if (await users.EmailExistsAsync(normalized, ct))
                    throw new ConflictException("Ez az e-mail cím már regisztrálva van.");
                user.Email = normalized;
            }

            await unitOfWork.SaveChangesAsync(ct);
            return UserDto.From(user);
        }
    }
}
