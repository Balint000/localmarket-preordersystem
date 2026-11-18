using localmarket_preordersystem.Application.Users.ChangeMyPassword;
using localmarket_preordersystem.Application.Users.GetMyProfile;
using localmarket_preordersystem.Application.Users.UpdateMyProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Controllers;

/// <summary>A bejelentkezett felhasználó saját profilja (bármely szerepkör).</summary>
[ApiController]
[Authorize]
[Route("api/me")]
public class ProfileController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(GetMyProfileHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(ct));

    [HttpPut]
    public async Task<IActionResult> Update(UpdateMyProfileCommand command, UpdateMyProfileHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(command, ct));

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword(ChangeMyPasswordCommand command, ChangeMyPasswordHandler handler, CancellationToken ct)
    {
        await handler.HandleAsync(command, ct);
        return NoContent();
    }
}
