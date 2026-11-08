using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Categories.DeleteCategory
{
    public sealed record DeleteCategoryCommand(int CategoryId);

    /// <summary>
    /// Kategória törlése. Csak piacvezető; naplózott. Használatban lévő kategória nem törölhető:
    /// a termékeknek legalább egy kategóriába kell tartozniuk (ld. Product invariáns).
    /// </summary>
    public sealed class DeleteCategoryHandler(ICategoryRepository categories, IAuditLogRepository audit, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task HandleAsync(DeleteCategoryCommand command, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Admin);

            var category = await categories.GetByIdAsync(command.CategoryId, ct)
                ?? throw new NotFoundException($"Nem található kategória: {command.CategoryId}.");

            if (await categories.IsInUseAsync(category.Id, ct))
                throw new ConflictException("A kategóriához még tartoznak termékek, ezért nem törölhető. Előbb helyezd át a termékeket másik kategóriába.");

            var before = CategoryDto.From(category);
            categories.Remove(category);
            await audit.RecordAsync(currentUser, "CategoryDeleted", "Category", before.Id, before, null, ct);
            await uow.SaveChangesAsync(ct);
        }
    }
}
