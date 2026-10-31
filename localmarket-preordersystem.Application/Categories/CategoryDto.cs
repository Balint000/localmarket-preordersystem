using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Categories
{
    public sealed record CategoryDto(int Id, string Name, string Description)
    {
        public static CategoryDto From(Category c) => new(c.Id, c.Name, c.Description);
    }
}
