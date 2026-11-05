using localmarket_preordersystem.Application.Auth.Login;
using localmarket_preordersystem.Application.Users.RegisterCustomer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command, LoginHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(command, ct));

    [HttpPost("register-customer")]
    public async Task<IActionResult> RegisterCustomer(RegisterCustomerCommand command, RegisterCustomerHandler handler, CancellationToken ct)
        => Ok(new { id = await handler.HandleAsync(command, ct) });
}
