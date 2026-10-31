using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Products.CreateProduct
{
    public sealed record CreateProductCommand(string Name, string? Description, IReadOnlyList<int> CategoryIds, IReadOnlyList<Allergy>? Allergies);

    public static class CreateProductValidator
    {
        public static void Validate(CreateProductCommand c) => new ValidationBuilder()
            .Required(nameof(c.Name), c.Name, "A termék nevének megadása kötelező.")
            .MaxLength(nameof(c.Name), c.Name, 100)
            .MaxLength(nameof(c.Description), c.Description, 500)
            .Must(nameof(c.CategoryIds), c.CategoryIds is { Count: > 0 }, "Legalább egy kategória megadása kötelező.")
            .Must(nameof(c.Allergies), c.Allergies is null || c.Allergies.All(Enum.IsDefined), "Érvénytelen allergén.")
            .ThrowIfInvalid();
    }
}
