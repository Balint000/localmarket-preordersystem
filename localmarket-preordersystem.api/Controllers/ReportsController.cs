using localmarket_preordersystem.Application.Reports.CancellationRate;
using localmarket_preordersystem.Application.Reports.TopProducts;
using localmarket_preordersystem.Application.Reports.UnclaimedOrders;
using localmarket_preordersystem.Application.Reports.VendorRevenue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    [HttpGet("top-products")]
    public async Task<IActionResult> TopProducts([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int top, TopProductsHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new TopProductsQuery(from, to, top == 0 ? 10 : top), ct));

    [HttpGet("cancellation-rate")]
    public async Task<IActionResult> CancellationRate([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationRateHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new CancellationRateQuery(from, to), ct));

    [HttpGet("vendor-revenue")]
    public async Task<IActionResult> VendorRevenue([FromQuery] DateTime? from, [FromQuery] DateTime? to, VendorRevenueHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new VendorRevenueQuery(from, to), ct));

    [HttpGet("unclaimed-orders")]
    public async Task<IActionResult> Unclaimed([FromQuery] DateTime? from, [FromQuery] DateTime? to, UnclaimedOrdersHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new UnclaimedOrdersQuery(from, to), ct));
}
