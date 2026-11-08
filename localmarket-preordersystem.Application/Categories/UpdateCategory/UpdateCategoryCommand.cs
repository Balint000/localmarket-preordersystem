using localmarket_preordersystem.Application.Common.Validation;

namespace localmarket_preordersystem.Application.Categories.UpdateCategory
{
    public sealed record UpdateCategoryCommand(int CategoryId, string Name, string? Description);

    public static class UpdateCategoryValidator
    {
        public static void Validate(UpdateCategoryCommand c)
        {
            new ValidationBuilder()
                .Positive(nameof(c.CategoryId), c.CategoryId, "Érvénytelen kategória azonosító.")
                .Required(nameof(c.Name), c.Name, "A kategória nevének megadása kötelező.")
                .MaxLength(nameof(c.Name), c.Name, 100)
                .MaxLength(nameof(c.Description), c.Description, 500)
                .ThrowIfInvalid();
        }
    }
}
