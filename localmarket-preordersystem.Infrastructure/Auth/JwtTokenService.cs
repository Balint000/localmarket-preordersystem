using System.Security.Claims;
using System.Text;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace localmarket_preordersystem.Infrastructure.Auth;

internal sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public AuthToken GenerateToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = clock.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_options.ExpiryMinutes);

        var claims = new Dictionary<string, object>
        {
            [JwtClaimNames.UserId] = user.Id.ToString(),
            [JwtClaimNames.Role] = user.Role.ToString(),
            ["email"] = user.Email
        };

        if (user.Role == Role.Producer && user.ProducerId > 0)
            claims[JwtClaimNames.ProducerId] = user.ProducerId.ToString();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Claims = claims,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AuthToken(token, expires);
    }
}
