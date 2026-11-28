using System.Text;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.ValueObject;
using localmarket_preordersystem.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace localmarket_preordersystem.api.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddApiAuth(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtAccessor) =>
            {
                var jwt = jwtAccessor.Value;
                if (string.IsNullOrWhiteSpace(jwt.Secret) || jwt.Secret.Length < 32)
                    throw new InvalidOperationException("A Jwt:Secret hiányzik vagy rövidebb 32 karakternél.");

                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtClaimNames.UserId,
                    RoleClaimType = JwtClaimNames.Role
                };

                // A token önmagában csak a lejáratáig érvényes; minden kérésnél megnézzük, hogy a fiók
                // létezik-e még, aktív-e, és a szerepköre/árus-profilja egyezik-e a tokenben lévővel.
                bearer.Events = new JwtBearerEvents { OnTokenValidated = ValidateUserStateAsync };
            });

        services.AddAuthorization();
        services.AddApiAuthorization();
        return services;
    }

    /// <summary>
    /// Letiltott, törölt vagy azóta más szerepkörű felhasználó tokenjét elutasítjuk (401). A Blazor kliens
    /// 401-re kijelentkeztet, így a letiltás a következő kérésnél érvényesül, nem csak a token lejártakor.
    /// </summary>
    private static async Task ValidateUserStateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (principal is null || !int.TryParse(principal.FindFirst(JwtClaimNames.UserId)?.Value, out var userId))
        {
            context.Fail("A token nem tartalmaz érvényes felhasználó-azonosítót.");
            return;
        }

        var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        var info = await users.GetAccessInfoAsync(userId, context.HttpContext.RequestAborted);
        if (info is null || !info.IsActive)
        {
            context.Fail("A felhasználói fiók nem létezik vagy le van tiltva.");
            return;
        }

        var tokenRole = principal.FindFirst(JwtClaimNames.Role)?.Value;
        var tokenProducerId = principal.FindFirst(JwtClaimNames.ProducerId)?.Value;
        // a JwtTokenService csak Producer szerepkörnél és létező profilnál ír producerId claimet
        var expectedProducerId = info.Role == Role.Producer && info.ProducerId > 0 ? info.ProducerId.ToString() : null;
        if (!string.Equals(tokenRole, info.Role.ToString(), StringComparison.Ordinal) || tokenProducerId != expectedProducerId)
            context.Fail("A fiók jogosultságai megváltoztak, jelentkezz be újra.");
    }
}
