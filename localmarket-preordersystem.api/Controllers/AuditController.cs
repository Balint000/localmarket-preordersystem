using localmarket_preordersystem.api.Contracts.Common;
using localmarket_preordersystem.api.Extensions;
using localmarket_preordersystem.Application.Audit.ListAuditLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Controllers;

[ApiController]
[Authorize(Policy = Policies.Manager)]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    // FE: opcionális szűrés entityName (pl. "Order") és entityId szerint; a lapozás adatbázis-szinten történik.
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? entityName, [FromQuery] int? entityId, [FromQuery] PageRequest paging, ListAuditLogsHandler handler, CancellationToken ct)
    {
        var page = await handler.HandleAsync(new ListAuditLogsQuery(entityName, entityId, paging.Page, paging.PageSize), ct);
        return Ok(new PagedResult<AuditLogDto>(page.Items, page.Total, paging.Page, paging.PageSize));
    }
}
