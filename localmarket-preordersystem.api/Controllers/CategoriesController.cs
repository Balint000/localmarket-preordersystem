using localmarket_preordersystem.api.Extensions;
using localmarket_preordersystem.Application.Categories.CreateCategory;
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
}
