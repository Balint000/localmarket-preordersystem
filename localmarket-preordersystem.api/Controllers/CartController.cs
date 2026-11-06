using localmarket_preordersystem.Application.Cart.AddToCart;
using localmarket_preordersystem.Application.Cart.GetCart;
using localmarket_preordersystem.Application.Cart.RemoveFromCart;
using localmarket_preordersystem.Application.Cart.UpdateCartItem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Controllers;

public sealed record QuantityRequest(decimal Quantity);

[ApiController]
[Authorize]
[Route("api/cart")]
public class CartController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(GetCartHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(ct));

    [HttpPost("items")]
    public async Task<IActionResult> Add(AddToCartCommand command, AddToCartHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(command, ct));

    [HttpPut("items/{productId:int}")]
    public async Task<IActionResult> Update(int productId, QuantityRequest body, UpdateCartItemHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new UpdateCartItemCommand(productId, body.Quantity), ct));

    [HttpDelete("items/{productId:int}")]
    public async Task<IActionResult> Remove(int productId, RemoveFromCartHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new RemoveFromCartCommand(productId), ct));
}
