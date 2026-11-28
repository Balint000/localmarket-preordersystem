using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Markets.CreateMarket
{
    /// <param name="OpensAt">Nyitás (alapból 07:00).</param>
    /// <param name="ClosesAt">Zárás (alapból 13:00).</param>
    public sealed record CreateMarketCommand(string Name, string Address, IReadOnlyList<DayOfWeek> OpeningDays, double? Latitude, double? Longitude, TimeOnly? OpensAt = null, TimeOnly? ClosesAt = null);

    public static class CreateMarketValidator
    {
        public static void Validate(CreateMarketCommand c)
        {
            new ValidationBuilder()
                .Required(nameof(c.Name), c.Name, "A piac nevének megadása kötelező.")
                .MaxLength(nameof(c.Name), c.Name, 100)
                .Required(nameof(c.Address), c.Address, "A piac címének megadása kötelező.")
                .MaxLength(nameof(c.Address), c.Address, 500)
                .Must(nameof(c.OpeningDays), c.OpeningDays is not null && c.OpeningDays.All(Enum.IsDefined), "Érvénytelen nyitvatartási nap.")
                .Must(nameof(c.ClosesAt), (c.ClosesAt ?? Market.DefaultClosesAt) > (c.OpensAt ?? Market.DefaultOpensAt), "A zárás időpontja a nyitás után kell legyen.")
                .ThrowIfInvalid();
        }
    }
}
