using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Validation;

namespace localmarket_preordersystem.Application.Users.ChangeMyPassword
{
    /// <summary>Jelszócsere: a jelenlegi jelszó ellenőrzése után az új jelszó hash-ét tároljuk.</summary>
    public sealed class ChangeMyPasswordHandler(IUserRepository users, IPasswordHasher hasher, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        public async Task HandleAsync(ChangeMyPasswordCommand command, CancellationToken ct = default)
        {
            new ValidationBuilder()
                .Required(nameof(command.CurrentPassword), command.CurrentPassword, "A jelenlegi jelszó megadása kötelező.")
                .Must(nameof(command.NewPassword), command.NewPassword is { Length: >= 8 }, "Az új jelszónak legalább 8 karakter hosszúnak kell lennie.")
                .MaxLength(nameof(command.NewPassword), command.NewPassword, 128)
                .Must(nameof(command.NewPassword), command.NewPassword != command.CurrentPassword, "Az új jelszó nem egyezhet meg a jelenlegivel.")
                .ThrowIfInvalid();

            var user = await users.GetByIdAsync(currentUser.RequireUserId(), ct)
                ?? throw new NotFoundException("A felhasználó nem található.");

            if (!hasher.Verify(command.CurrentPassword, user.PasswordHash))
                new ValidationBuilder().Add(nameof(command.CurrentPassword), "A jelenlegi jelszó hibás.").ThrowIfInvalid();

            user.ChangePasswordHash(hasher.Hash(command.NewPassword));
            await unitOfWork.SaveChangesAsync(ct);
        }
    }
}
