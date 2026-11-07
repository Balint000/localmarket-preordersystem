using localmarket_preordersystem.api.Contracts.Products;
using localmarket_preordersystem.api.Extensions;
using localmarket_preordersystem.api.Contracts.Common;
using localmarket_preordersystem.Application.Products.CreateProduct;
using localmarket_preordersystem.Application.Products.GetProduct;
using localmarket_preordersystem.Application.Products.SearchProducts;
using localmarket_preordersystem.Application.Products.SetWeeklyStock;
using localmarket_preordersystem.Application.Products.UpdateProduct;
using localmarket_preordersystem.Domain.ValueObject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Search(
        [FromQuery] string? text,
        [FromQuery] int? categoryId,
        [FromQuery] int? producerId,
        [FromQuery] Allergy[]? excludeAllergies,
        [FromQuery] PageRequest paging,
        SearchProductsHandler handler,
        CancellationToken ct)
        => Ok((await handler.HandleAsync(new SearchProductsQuery(text, categoryId, producerId, excludeAllergies), ct)).ToPaged(paging));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(int id, GetProductHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Policies.Producer)]
    public async Task<IActionResult> Create(CreateProductCommand command, CreateProductHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(command, ct));

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.Producer)]
    public async Task<IActionResult> Update(int id, UpdateProductRequest body, UpdateProductHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(
            new UpdateProductCommand(id, body.Name, body.Description, body.CategoryIds, body.Allergies), ct));

    // FE: Quantity 0 means "sold out for the week"; WeekDate must be a market day.
    [HttpPut("{id:int}/stock")]
    [Authorize(Policy = Policies.Producer)]
    public async Task<IActionResult> SetStock(int id, StockRequest body, SetWeeklyStockHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(
            new SetWeeklyStockCommand(id, body.WeekDate, body.Quantity, body.Unit, body.UnitPrice), ct));
}
