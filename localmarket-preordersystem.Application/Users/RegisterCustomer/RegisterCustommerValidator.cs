using System.Text.RegularExpressions;
using localmarket_preordersystem.Application.Common.Exceptions;

namespace localmarket_preordersystem.Application.Users.RegisterCustomer
{
    /// <summary>
    /// Kézzel írt, minimális input-validáció — nem FluentValidation, mert ennél a
    /// méretnél egy csomagfüggőség nem indokolt (ld. ValidationException kommentje).
    /// </summary>
    public static class RegisterCustomerValidator
    {
        private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public static void Validate(RegisterCustomerCommand command)
        {
            var errors = new Dictionary<string, List<string>>();

            void AddError(string field, string message)
            {
                if (!errors.TryGetValue(field, out var list))
                    errors[field] = list = new List<string>();
                list.Add(message);
            }

            if (string.IsNullOrWhiteSpace(command.FirstName))
                AddError(nameof(command.FirstName), "A keresztnév megadása kötelező.");

            if (string.IsNullOrWhiteSpace(command.LastName))
                AddError(nameof(command.LastName), "A vezetéknév megadása kötelező.");

            if (string.IsNullOrWhiteSpace(command.Email) || !EmailPattern.IsMatch(command.Email))
                AddError(nameof(command.Email), "Érvénytelen e-mail cím formátum.");

            if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < 8)
                AddError(nameof(command.Password), "A jelszónak legalább 8 karakter hosszúnak kell lennie.");

            if (errors.Count > 0)
                throw new ValidationException(errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }
    }
}