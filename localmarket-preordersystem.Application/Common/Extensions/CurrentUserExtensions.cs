using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Common.Extensions
{
    /// <summary>
    /// Finom jogosultság-ellenőrzés a handlerekben. A durva (szerepkör) szűrést az API
    /// [Authorize(Roles=...)] végzi, de a handler önmagában is véd (nem csak az API-ra támaszkodik).
    /// </summary>
    public static class CurrentUserExtensions
    {
        public static int RequireUserId(this ICurrentUserService user)
            => user.IsAuthenticated && user.UserId is int id
                ? id
                : throw new AuthenticationException("A művelethez bejelentkezés szükséges.");

        public static void RequireRole(this ICurrentUserService user, params Role[] roles)
        {
            user.RequireUserId();
            if (user.Role is not Role role || !roles.Contains(role))
                throw new ForbiddenAccessException("Ehhez a művelethez nincs jogosultságod.");
        }

        public static int RequireProducerId(this ICurrentUserService user)
        {
            user.RequireRole(Role.Producer);
            return user.ProducerId ?? throw new ForbiddenAccessException("A felhasználóhoz nem tartozik árus-profil.");
        }

        public static bool IsManager(this ICurrentUserService user) => user.Role == Role.Admin;
    }
}
