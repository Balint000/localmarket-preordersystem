using System.ComponentModel.DataAnnotations;

namespace localmarket_preordersystem.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required, MinLength(32)]
    public string Secret { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = "localmarket";

    [Required]
    public string Audience { get; set; } = "localmarket-clients";

    [Range(1, 1440)]
    public int ExpiryMinutes { get; set; } = 60;
}
