using System.ComponentModel.DataAnnotations;

namespace IdentityService.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(32)]
    public string Secret { get; init; } = string.Empty;

    public string? Issuer { get; init; }
    public string? Audience { get; init; }

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 365)]
    public int RefreshTokenDays { get; init; } = 7;
}

