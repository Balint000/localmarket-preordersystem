using localmarket_preordersystem.Application.Common.Validation;

namespace localmarket_preordersystem.Application.Categories.CreateCategory
{
    public sealed record CreateCategoryCommand(string Name, string? Description);

    public static class CreateCategoryValidator
    {
        public static void Validate(CreateCategoryCommand c)
        {
            new ValidationBuilder()
                .Required(nameof(c.Name), c.Name, "A kategória nevének megadása kötelező.")
                .MaxLength(nameof(c.Name), c.Name, 100)
                .MaxLength(nameof(c.Description), c.Description, 500)
                .ThrowIfInvalid();
        }
    }
}
