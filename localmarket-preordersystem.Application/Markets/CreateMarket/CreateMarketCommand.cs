using localmarket_preordersystem.Application.Common.Validation;

namespace localmarket_preordersystem.Application.Markets.CreateMarket
{
    public sealed record CreateMarketCommand(string Name, string Address, IReadOnlyList<DayOfWeek> OpeningDays, double? Latitude, double? Longitude);

    public static class CreateMarketValidator
    {
        public static void Validate(CreateMarketCommand c)
        {
            new ValidationBuilder()
                .Required(nameof(c.Name), c.Name, "A piac nevének megadása kötelező.")
                .MaxLength(nameof(c.Name), c.Name, 100)
                .Required(nameof(c.Address), c.Address, "A piac címének megadása kötelező.")
                .Must(nameof(c.OpeningDays), c.OpeningDays is not null && c.OpeningDays.All(Enum.IsDefined), "Érvénytelen nyitvatartási nap.")
                .ThrowIfInvalid();
        }
    }
}
