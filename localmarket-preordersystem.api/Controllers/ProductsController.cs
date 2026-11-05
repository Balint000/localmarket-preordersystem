using localmarket_preordersystem.Application.Products.CreateProduct;
using localmarket_preordersystem.Application.Products.GetProduct;
using localmarket_preordersystem.Application.Products.SetWeeklyStock;
using localmarket_preordersystem.Application.Products.UpdateProduct;
using localmarket_preordersystem.Domain.ValueObject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Controllers;

public sealed record UpdateProductRequest(string Name, string? Description, IReadOnlyList<int> CategoryIds, IReadOnlyList<Allergy>? Allergies);
public sealed record StockRequest(DateTime WeekDate, decimal Quantity, Units Unit, decimal UnitPrice);

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(int id, GetProductHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(id, ct));

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(CreateProductCommand command, CreateProductHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(command, ct));

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, UpdateProductRequest body, UpdateProductHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(
            new UpdateProductCommand(id, body.Name, body.Description, body.CategoryIds, body.Allergies), ct));

    [HttpPut("{id:int}/stock")]
    [Authorize]
    public async Task<IActionResult> SetStock(int id, StockRequest body, SetWeeklyStockHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(
            new SetWeeklyStockCommand(id, body.WeekDate, body.Quantity, body.Unit, body.UnitPrice), ct));
}
