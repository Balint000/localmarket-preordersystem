using localmarket_preordersystem.api.Extensions;
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
    // FE: format=Json|Csv; response is a file download (use blob).
    [HttpGet("orders/{id:int}")]
    public async Task<IActionResult> OrderSummary(int id, [FromQuery] ExportFormat format, ExportOrderSummaryHandler handler, CancellationToken ct)
    {
        if (!Enum.IsDefined(format)) return InvalidFormat();
        var file = await handler.HandleAsync(new ExportOrderSummaryQuery(id, format == 0 ? ExportFormat.Json : format), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("picking-list")]
    [Authorize(Policy = Policies.ProducerOrManager)]
    public async Task<IActionResult> PickingList([FromQuery] DateTime? date, [FromQuery] ExportFormat format, ExportPickingListHandler handler, CancellationToken ct)
    {
        if (date is null) return Problem(statusCode: 400, title: "Hiányzó dátum", detail: "A 'date' paraméter kötelező.");
        if (!Enum.IsDefined(format)) return InvalidFormat();
        var file = await handler.HandleAsync(new ExportPickingListQuery(date.Value, format == 0 ? ExportFormat.Json : format), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    private ObjectResult InvalidFormat()
        => Problem(statusCode: 400, title: "Érvénytelen formátum", detail: "Használd: Json vagy Csv.");
}
