using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Categories.UpdateCategory
{
    /// <summary>Kategória átnevezése / leírásának módosítása. Csak piacvezető; naplózott.</summary>
    public sealed class UpdateCategoryHandler(ICategoryRepository categories, IAuditLogRepository audit, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<CategoryDto> HandleAsync(UpdateCategoryCommand command, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Admin);
            UpdateCategoryValidator.Validate(command);

            var category = await categories.GetByIdAsync(command.CategoryId, ct)
                ?? throw new NotFoundException($"Nem található kategória: {command.CategoryId}.");

            if (await categories.NameExistsAsync(command.Name.Trim(), category.Id, ct))
                throw new ConflictException("Ilyen nevű kategória már létezik.");

            var before = CategoryDto.From(category);
            category.Update(command.Name, command.Description ?? string.Empty);
            var after = CategoryDto.From(category);

            await audit.RecordAsync(currentUser, "CategoryUpdated", "Category", category.Id, before, after, ct);
            await uow.SaveChangesAsync(ct);
            return after;
        }
    }
}
