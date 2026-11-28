using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Categories.CreateCategory
{
    public sealed class CreateCategoryHandler(ICategoryRepository categories, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<CategoryDto> HandleAsync(CreateCategoryCommand command, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Admin);
            CreateCategoryValidator.Validate(command);

            if (await categories.NameExistsAsync(command.Name.Trim(), null, ct))
                throw new ConflictException("Ilyen nevű kategória már létezik.");

            var category = Category.Create(command.Name, command.Description ?? string.Empty);
            await categories.AddAsync(category, ct);
            await uow.SaveChangesAsync(ct);
            return CategoryDto.From(category);
        }
    }
}
