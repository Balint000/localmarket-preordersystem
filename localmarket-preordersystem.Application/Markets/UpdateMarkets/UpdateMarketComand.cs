using localmarket_preordersystem.Application.Common.Validation;

namespace localmarket_preordersystem.Application.Markets.UpdateMarket
{
    public sealed record UpdateMarketCommand(int MarketId, string Name, string Address, IReadOnlyList<DayOfWeek> OpeningDays, double? Latitude, double? Longitude, bool IsActive);

    public static class UpdateMarketValidator
    {
        public static void Validate(UpdateMarketCommand c)
        {
            new ValidationBuilder()
                .Positive(nameof(c.MarketId), c.MarketId, "Érvénytelen piac azonosító.")
                .Required(nameof(c.Name), c.Name, "A piac nevének megadása kötelező.")
                .MaxLength(nameof(c.Name), c.Name, 100)
                .Required(nameof(c.Address), c.Address, "A piac címének megadása kötelező.")
                .Must(nameof(c.OpeningDays), c.OpeningDays is not null && c.OpeningDays.All(Enum.IsDefined), "Érvénytelen nyitvatartási nap.")
                .ThrowIfInvalid();
        }
    }
}
