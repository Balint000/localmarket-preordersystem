using localmarket_preordersystem.Application.Auth.Login;
using localmarket_preordersystem.Application.Users.RegisterCustomer;
using localmarket_preordersystem.Application.Users.RegisterProducer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace localmarket_preordersystem.api.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("auth")]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // FE: store token; on 429 show "too many attempts, wait a minute".
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command, LoginHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(command, ct));

    // FE: show field errors from problem.errors (email, password, phone).
    [HttpPost("register-customer")]
    public async Task<IActionResult> RegisterCustomer(RegisterCustomerCommand command, RegisterCustomerHandler handler, CancellationToken ct)
        => Ok(new { id = await handler.HandleAsync(command, ct) });

    // FE: new producers stay Pending until a manager approves them.
    [HttpPost("register-producer")]
    public async Task<IActionResult> RegisterProducer(RegisterProducerCommand command, RegisterProducerHandler handler, CancellationToken ct)
        => Ok(new { id = await handler.HandleAsync(command, ct) });
}
