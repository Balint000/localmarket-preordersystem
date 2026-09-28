using localmarket_preordersystem.Application.Common.Exceptions;

namespace localmarket_preordersystem.Application.Auth.Login
{
    public static class LoginValidator
    {
        public static void Validate(LoginCommand command)
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(command.Email))
                errors[nameof(command.Email)] = new[] { "Az e-mail cím megadása kötelező." };

            if (string.IsNullOrWhiteSpace(command.Password))
                errors[nameof(command.Password)] = new[] { "A jelszó megadása kötelező." };

            if (errors.Count > 0)
                throw new ValidationException(errors);
        }
    }
}