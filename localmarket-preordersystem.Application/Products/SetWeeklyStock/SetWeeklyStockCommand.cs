using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Products.SetWeeklyStock
{
    /// <summary>A megadott dátumot tartalmazó hét heti kínálata (mennyiség, mértékegység, ár). Létrehoz vagy módosít.</summary>
    public sealed record SetWeeklyStockCommand(int ProductId, DateTime WeekDate, decimal Quantity, Units Unit, decimal UnitPrice);

    public static class SetWeeklyStockValidator
    {
        public static void Validate(SetWeeklyStockCommand c) => new ValidationBuilder()
            .Positive(nameof(c.ProductId), c.ProductId, "Érvénytelen termék azonosító.")
            .NonNegative(nameof(c.Quantity), c.Quantity, "A mennyiség nem lehet negatív (0 = erre a hétre elfogyott).")
            .Must(nameof(c.Quantity), c.Quantity <= 1_000_000m, "A mennyiség legfeljebb 1 000 000 lehet.")
            .Positive(nameof(c.UnitPrice), c.UnitPrice, "Az egységárnak pozitívnak kell lennie.")
            .Must(nameof(c.UnitPrice), c.UnitPrice <= 10_000_000m, "Az egységár legfeljebb 10 000 000 lehet.")
            .Must(nameof(c.Unit), Enum.IsDefined(c.Unit), "Érvénytelen mértékegység.")
            .ThrowIfInvalid();
    }
}
