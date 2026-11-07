using localmarket_preordersystem.api.Extensions;
using localmarket_preordersystem.Application.Reports.CancellationRate;
using localmarket_preordersystem.Application.Reports.TopProducts;
using localmarket_preordersystem.Application.Reports.UnclaimedOrders;
using localmarket_preordersystem.Application.Reports.VendorRevenue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Controllers;

[ApiController]
[Authorize(Policy = Policies.Manager)]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    // FE: 'to' as a date-only value includes that whole day; 'from' must be <= 'to'; top is 1..100 (default 10).
    [HttpGet("top-products")]
    public async Task<IActionResult> TopProducts([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int top, TopProductsHandler handler, CancellationToken ct)
    {
        if (from > to) return InvalidRange();
        if (top is < 0 or > 100) return Problem(statusCode: 400, title: "Érvénytelen érték", detail: "A 'top' 1 és 100 közötti legyen.");
        return Ok(await handler.HandleAsync(new TopProductsQuery(from, EndOfDay(to), top == 0 ? 10 : top), ct));
    }

    [HttpGet("cancellation-rate")]
    public async Task<IActionResult> CancellationRate([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationRateHandler handler, CancellationToken ct)
        => from > to ? InvalidRange() : Ok(await handler.HandleAsync(new CancellationRateQuery(from, EndOfDay(to)), ct));

    [HttpGet("vendor-revenue")]
    public async Task<IActionResult> VendorRevenue([FromQuery] DateTime? from, [FromQuery] DateTime? to, VendorRevenueHandler handler, CancellationToken ct)
        => from > to ? InvalidRange() : Ok(await handler.HandleAsync(new VendorRevenueQuery(from, EndOfDay(to)), ct));

    [HttpGet("unclaimed-orders")]
    public async Task<IActionResult> Unclaimed([FromQuery] DateTime? from, [FromQuery] DateTime? to, UnclaimedOrdersHandler handler, CancellationToken ct)
        => from > to ? InvalidRange() : Ok(await handler.HandleAsync(new UnclaimedOrdersQuery(from, EndOfDay(to)), ct));

    private static DateTime? EndOfDay(DateTime? to)
        => to is { } d && d.TimeOfDay == TimeSpan.Zero ? d.Date.AddDays(1).AddTicks(-1) : to;

    private ObjectResult InvalidRange()
        => Problem(statusCode: 400, title: "Érvénytelen intervallum", detail: "A 'from' nem lehet későbbi, mint a 'to'.");
}
