using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Export.ExportOrderSummary;
using localmarket_preordersystem.Application.Export.ExportPickingList;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Controllers;

[ApiController]
[Authorize]
[Route("api/export")]
public class ExportController : ControllerBase
{
    [HttpGet("orders/{id:int}")]
    public async Task<IActionResult> OrderSummary(int id, [FromQuery] ExportFormat format, ExportOrderSummaryHandler handler, CancellationToken ct)
    {
        var file = await handler.HandleAsync(new ExportOrderSummaryQuery(id, format == 0 ? ExportFormat.Json : format), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("picking-list")]
    public async Task<IActionResult> PickingList([FromQuery] DateTime date, [FromQuery] ExportFormat format, ExportPickingListHandler handler, CancellationToken ct)
    {
        var file = await handler.HandleAsync(new ExportPickingListQuery(date, format == 0 ? ExportFormat.Json : format), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
