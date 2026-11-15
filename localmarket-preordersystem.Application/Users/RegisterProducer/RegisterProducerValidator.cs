using localmarket_preordersystem.Application.Common.Validation;

namespace localmarket_preordersystem.Application.Users.RegisterProducer
{
    public static class RegisterProducerValidator
    {
        public static void Validate(RegisterProducerCommand command) => new ValidationBuilder()
            .Required(nameof(command.FirstName), command.FirstName, "A keresztnév megadása kötelező.")
            .Required(nameof(command.LastName), command.LastName, "A vezetéknév megadása kötelező.")
            .Email(nameof(command.Email), command.Email)
            .Must(nameof(command.Password), HasValidLength(command.Password), "A jelszónak legalább 8 karakter hosszúnak kell lennie.")
            .Required(nameof(command.ProducerName), command.ProducerName, "Az árus/gazdaság nevének megadása kötelező.")
            .Must(nameof(command.StallNumber), command.StallNumber >= 0, "A standszám nem lehet negatív.")
            .ThrowIfInvalid();

        private static bool HasValidLength(string? password) => !string.IsNullOrWhiteSpace(password) && password.Length >= 8;
    }
}
