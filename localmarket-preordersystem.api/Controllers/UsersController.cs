using localmarket_preordersystem.api.Extensions;
using localmarket_preordersystem.Application.Users.GetMyProfile;
using localmarket_preordersystem.Application.Users.ListUsers;
using localmarket_preordersystem.Application.Users.SetUserActive;
using localmarket_preordersystem.Domain.ValueObject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    // Manager only. search matches e-mail and name.
    [HttpGet]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> List(
        [FromQuery] Role? role,
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        ListUsersHandler handler,
        CancellationToken ct)
        => Ok(await handler.HandleAsync(new ListUsersQuery(role, isActive, search), ct));

    // Manager only. A letiltás azonnal hat (a token-ellenőrzés minden kérésnél megnézi a fiók állapotát).
    [HttpPost("{id:int}/deactivate")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Deactivate(int id, SetUserActiveHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new SetUserActiveCommand(id, false), ct));

    [HttpPost("{id:int}/activate")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Activate(int id, SetUserActiveHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(new SetUserActiveCommand(id, true), ct));

    // Any signed-in user (incl. Pending producers).
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(GetMyProfileHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(ct));
}
