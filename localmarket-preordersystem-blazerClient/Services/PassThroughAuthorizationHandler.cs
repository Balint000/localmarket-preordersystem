using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace localmarket_preordersystem.Services;

/// <summary>
/// A bejelentkezés a Blazor körben (circuit) él, a HTTP-kérés nem hordoz hitelesítést (nincs auth-séma).
/// Ezért az endpoint-szintű [Authorize] ne indítson challenge-et: a jogosultságot az AuthorizeRouteView
/// (Routes.razor) és a komponensek [Authorize] attribútuma dönti el.
/// </summary>
public sealed class PassThroughAuthorizationHandler : IAuthorizationMiddlewareResultHandler
{
    public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        => next(context);
}
