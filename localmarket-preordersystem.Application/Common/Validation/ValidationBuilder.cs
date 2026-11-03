using System.Text.RegularExpressions;
using localmarket_preordersystem.Application.Common.Exceptions;

namespace localmarket_preordersystem.Application.Common.Validation
{
    /// <summary>Kis, kézzel írt validációs segéd (nincs FluentValidation): hibákat gyűjt, a végén egyben dob.</summary>
    public sealed class ValidationBuilder
    {
        private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private readonly Dictionary<string, List<string>> _errors = new();

        public ValidationBuilder Add(string field, string message)
        {
            if (!_errors.TryGetValue(field, out var list))
                _errors[field] = list = new List<string>();
            list.Add(message);
            return this;
        }

        public ValidationBuilder Required(string field, string? value, string message)
            => string.IsNullOrWhiteSpace(value) ? Add(field, message) : this;

        public ValidationBuilder MaxLength(string field, string? value, int max)
            => value is not null && value.Trim().Length > max ? Add(field, $"Legfeljebb {max} karakter lehet.") : this;

        public ValidationBuilder Email(string field, string? value)
            => string.IsNullOrWhiteSpace(value) || !EmailPattern.IsMatch(value) ? Add(field, "Érvénytelen e-mail cím formátum.") : this;

        public ValidationBuilder Positive(string field, decimal value, string message)
            => value <= 0 ? Add(field, message) : this;

        public ValidationBuilder Positive(string field, int value, string message)
            => value <= 0 ? Add(field, message) : this;

        public ValidationBuilder Must(string field, bool condition, string message)
            => condition ? this : Add(field, message);

        public bool IsValid => _errors.Count == 0;

        public void ThrowIfInvalid()
        {
            if (_errors.Count > 0)
                throw new ValidationException(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }
    }
}
