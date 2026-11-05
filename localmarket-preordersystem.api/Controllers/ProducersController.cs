using localmarket_preordersystem.Application.Producers.ApproveProducer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Controllers;

[ApiController]
[Authorize]
[Route("api/producers")]
public class ProducersController : ControllerBase
{
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, ApproveProducerHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new ApproveProducerCommand(id), ct));
}
