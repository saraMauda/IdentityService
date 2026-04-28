using IdentityService.Data.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace IdentityService.Security;

public sealed class TokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string token, DateTime expiresAtUtc) CreateAccessToken(User user, DateTime nowUtc)
    {
        var expiresAtUtc = nowUtc.AddMinutes(_options.AccessTokenMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("role", user.Role.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAtUtc,
            Issuer = string.IsNullOrWhiteSpace(_options.Issuer) ? null : _options.Issuer,
            Audience = string.IsNullOrWhiteSpace(_options.Audience) ? null : _options.Audience,
            SigningCredentials = creds,
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(descriptor);
        return (token, expiresAtUtc);
    }

    public DateTime GetRefreshTokenExpiresAtUtc(DateTime nowUtc)
        => nowUtc.AddDays(_options.RefreshTokenDays);

    public DateTime GetPasswordResetTokenExpiresAtUtc(DateTime nowUtc)
        => nowUtc.AddMinutes(30);

    public string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Base64UrlEncoder.Encode(bytes);
    }

    public string CreatePasswordResetToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        return Base64UrlEncoder.Encode(bytes);
    }

    public static string HashRefreshToken(string refreshToken)
    {
        var bytes = Encoding.UTF8.GetBytes(refreshToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}

