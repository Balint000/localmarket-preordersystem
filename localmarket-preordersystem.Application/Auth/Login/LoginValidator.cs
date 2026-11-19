using localmarket_preordersystem.Application.Common.Validation;

namespace localmarket_preordersystem.Application.Auth.Login
{
    public static class LoginValidator
    {
        public static void Validate(LoginCommand command) => new ValidationBuilder()
            .Required(nameof(command.Email), command.Email, "Az e-mail cím megadása kötelező.")
            .Required(nameof(command.Password), command.Password, "A jelszó megadása kötelező.")
            .ThrowIfInvalid();
    }
}
