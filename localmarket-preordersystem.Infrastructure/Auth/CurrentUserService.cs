using System.Security.Claims;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.ValueObject;
using Microsoft.AspNetCore.Http;

namespace localmarket_preordersystem.Infrastructure.Auth;

internal sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int? UserId => ReadInt(JwtClaimNames.UserId);

    public Role? Role =>
        Enum.TryParse<Role>(Principal?.FindFirst(JwtClaimNames.Role)?.Value, out var role) ? role : null;

    public int? ProducerId => ReadInt(JwtClaimNames.ProducerId);

    private int? ReadInt(string claimType)
        => int.TryParse(Principal?.FindFirst(claimType)?.Value, out var value) ? value : null;
}
