using localmarket_preordersystem.Application.Common.Validation;

namespace localmarket_preordersystem.Application.Users.RegisterCustomer
{
    public static class RegisterCustomerValidator
    {
        public static void Validate(RegisterCustomerCommand command) => new ValidationBuilder()
            .Required(nameof(command.FirstName), command.FirstName, "A keresztnév megadása kötelező.")
            .Required(nameof(command.LastName), command.LastName, "A vezetéknév megadása kötelező.")
            .Email(nameof(command.Email), command.Email)
            .Must(nameof(command.Password), HasValidLength(command.Password), "A jelszónak legalább 8 karakter hosszúnak kell lennie.")
            .MaxLength(nameof(command.FirstName), command.FirstName, 100)
            .MaxLength(nameof(command.LastName), command.LastName, 100)
            .MaxLength(nameof(command.Email), command.Email, 320)
            .MaxLength(nameof(command.PhoneNumber), command.PhoneNumber, 30)
            .MaxLength(nameof(command.Password), command.Password, 128)
            .ThrowIfInvalid();

        private static bool HasValidLength(string? password) => !string.IsNullOrWhiteSpace(password) && password.Length >= 8;
    }
}
