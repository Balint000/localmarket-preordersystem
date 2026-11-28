using localmarket_preordersystem.Application.Common.Validation;

namespace localmarket_preordersystem.Application.Markets.UpdateMarket
{
    /// <param name="OpensAt">Nyitás; null = marad a jelenlegi.</param>
    /// <param name="ClosesAt">Zárás; null = marad a jelenlegi.</param>
    public sealed record UpdateMarketCommand(int MarketId, string Name, string Address, IReadOnlyList<DayOfWeek> OpeningDays, double? Latitude, double? Longitude, bool IsActive, TimeOnly? OpensAt = null, TimeOnly? ClosesAt = null);

    public static class UpdateMarketValidator
    {
        public static void Validate(UpdateMarketCommand c)
        {
            new ValidationBuilder()
                .Positive(nameof(c.MarketId), c.MarketId, "Érvénytelen piac azonosító.")
                .Required(nameof(c.Name), c.Name, "A piac nevének megadása kötelező.")
                .MaxLength(nameof(c.Name), c.Name, 100)
                .Required(nameof(c.Address), c.Address, "A piac címének megadása kötelező.")
                .MaxLength(nameof(c.Address), c.Address, 500)
                .Must(nameof(c.OpeningDays), c.OpeningDays is not null && c.OpeningDays.All(Enum.IsDefined), "Érvénytelen nyitvatartási nap.")
                .Must(nameof(c.ClosesAt), c.OpensAt is null || c.ClosesAt is null || c.ClosesAt > c.OpensAt, "A zárás időpontja a nyitás után kell legyen.")
                .ThrowIfInvalid();
        }
    }
}
