using localmarket_preordersystem.api.Extensions;
using localmarket_preordersystem.api.Contracts.Categories;
using localmarket_preordersystem.Application.Categories.CreateCategory;
using localmarket_preordersystem.Application.Categories.DeleteCategory;
using localmarket_preordersystem.Application.Categories.UpdateCategory;
using localmarket_preordersystem.Application.Categories.ListCategories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(ListCategoriesHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(ct));

    [HttpPost]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Create(CreateCategoryCommand command, CreateCategoryHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(command, ct));

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Update(int id, CategoryRequest body, UpdateCategoryHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new UpdateCategoryCommand(id, body.Name, body.Description), ct));

    // 409 if products still belong to the category.
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Delete(int id, DeleteCategoryHandler handler, CancellationToken ct)
    {
        await handler.HandleAsync(new DeleteCategoryCommand(id), ct);
        return NoContent();
    }
}
