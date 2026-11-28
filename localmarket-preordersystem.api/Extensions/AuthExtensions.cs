using System.Text;
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
            });

        services.AddAuthorization();
        services.AddApiAuthorization();
        return services;
    }
}
