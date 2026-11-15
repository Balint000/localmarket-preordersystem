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
            .Must(nameof(c.Quantity), c.Quantity >= 0, "A mennyiség nem lehet negatív.")
            .Positive(nameof(c.UnitPrice), c.UnitPrice, "Az egységárnak pozitívnak kell lennie.")
            .Must(nameof(c.Unit), Enum.IsDefined(c.Unit), "Érvénytelen mértékegység.")
            .ThrowIfInvalid();
    }
}
