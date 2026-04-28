using IdentityService.Contracts.Auth;
using IdentityService.Contracts.Events;
using IdentityService.Data;
using IdentityService.Data.Entities;
using IdentityService.Messaging;
using IdentityService.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IdentityContext _db;
    private readonly TokenService _tokens;
    private readonly IUserEventsPublisher _events;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthController(IdentityContext db, TokenService tokens, IUserEventsPublisher events)
    {
        _db = db;
        _tokens = tokens;
        _events = events;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;

        var exists = await _db.Users.AnyAsync(
            x => x.Email == request.Email || x.NationalId == request.NationalId,
            ct);

        if (exists)
        {
            return Conflict();
        }

        var user = new User
        {
            Email = request.Email,
            NationalId = request.NationalId,
            Role = request.Role,
            IsActive = true,
            CreatedAt = nowUtc,
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        await _events.PublishUserRegistered(
            new UserRegisteredEvent(
                user.UserId,
                user.Email,
                user.Role,
                user.CreatedAt),
            ct);

        if (user.Role == UserRole.Student)
        {
            await _events.PublishStudentRegistered(
                new StudentRegisteredEvent(
                    user.UserId,
                    user.CreatedAt,
                    user.CreatedAt.Year,
                    user.CreatedAt.Month),
                ct);
        }

        return CreatedAtAction(nameof(Register), new { userId = user.UserId }, null);
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;

        var user = await _db.Users.SingleOrDefaultAsync(x => x.NationalId == request.NationalId, ct);
        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verify == PasswordVerificationResult.Failed)
        {
            return Unauthorized();
        }

        var (accessToken, accessExpiresAtUtc) = _tokens.CreateAccessToken(user, nowUtc);
        var refreshToken = _tokens.CreateRefreshToken();
        var refreshHash = TokenService.HashRefreshToken(refreshToken);
        var refreshExpiresAtUtc = _tokens.GetRefreshTokenExpiresAtUtc(nowUtc);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.UserId,
            TokenHash = refreshHash,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = refreshExpiresAtUtc,
        });
        await _db.SaveChangesAsync(ct);

        return Ok(new TokenResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = accessExpiresAtUtc,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshExpiresAtUtc,
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken ct)
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(sub, out var userId))
        {
            return Unauthorized();
        }

        var user = await _db.Users.SingleOrDefaultAsync(x => x.UserId == userId && x.IsActive, ct);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new MeResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            Role = user.Role.ToString(),
        });
    }

    [HttpGet("admin-probe")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public IActionResult AdminProbe()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
        var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Ok(new
        {
            message = "Admin access granted",
            subject,
            role,
        });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;
        var oldHash = TokenService.HashRefreshToken(request.RefreshToken);

        var existing = await _db.RefreshTokens
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == oldHash, ct);

        if (existing is null ||
            existing.User is null ||
            !existing.User.IsActive ||
            existing.ExpiresAtUtc <= nowUtc ||
            existing.UsedAtUtc is not null ||
            existing.RevokedAtUtc is not null)
        {
            return Unauthorized();
        }

        var newRefreshToken = _tokens.CreateRefreshToken();
        var newHash = TokenService.HashRefreshToken(newRefreshToken);
        var newRefreshExpiresAtUtc = _tokens.GetRefreshTokenExpiresAtUtc(nowUtc);

        existing.UsedAtUtc = nowUtc;
        existing.RevokedAtUtc = nowUtc;
        existing.ReplacedByTokenHash = newHash;

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = existing.UserId,
            TokenHash = newHash,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = newRefreshExpiresAtUtc,
        });

        var (accessToken, accessExpiresAtUtc) = _tokens.CreateAccessToken(existing.User, nowUtc);
        await _db.SaveChangesAsync(ct);

        return Ok(new TokenResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = accessExpiresAtUtc,
            RefreshToken = newRefreshToken,
            RefreshTokenExpiresAtUtc = newRefreshExpiresAtUtc,
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var refreshHash = TokenService.HashRefreshToken(request.RefreshToken);

        var existing = await _db.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == refreshHash, ct);

        if (existing is null || existing.RevokedAtUtc is not null)
        {
            return Unauthorized();
        }

        existing.RevokedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;
        var refreshHash = TokenService.HashRefreshToken(request.RefreshToken);

        var session = await _db.RefreshTokens
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == refreshHash, ct);

        if (session is null ||
            session.User is null ||
            !session.User.IsActive ||
            session.ExpiresAtUtc <= nowUtc ||
            session.UsedAtUtc is not null ||
            session.RevokedAtUtc is not null)
        {
            return Unauthorized();
        }

        var verify = _passwordHasher.VerifyHashedPassword(session.User, session.User.PasswordHash, request.CurrentPassword);
        if (verify == PasswordVerificationResult.Failed)
        {
            return Unauthorized();
        }

        session.User.PasswordHash = _passwordHasher.HashPassword(session.User, request.NewPassword);

        var activeSessions = await _db.RefreshTokens
            .Where(x => x.UserId == session.UserId && x.RevokedAtUtc == null && x.UsedAtUtc == null && x.ExpiresAtUtc > nowUtc)
            .ToListAsync(ct);

        foreach (var activeSession in activeSessions)
        {
            activeSession.RevokedAtUtc = nowUtc;
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Email == request.Email && x.IsActive, ct);

        if (user is null)
        {
            return Accepted();
        }

        var resetToken = _tokens.CreatePasswordResetToken();
        var resetHash = TokenService.HashRefreshToken(resetToken);
        var expiresAtUtc = _tokens.GetPasswordResetTokenExpiresAtUtc(nowUtc);

        var existingTokens = await _db.PasswordResetTokens
            .Where(x => x.UserId == user.UserId && x.RevokedAtUtc == null && x.UsedAtUtc == null && x.ExpiresAtUtc > nowUtc)
            .ToListAsync(ct);

        foreach (var existingToken in existingTokens)
        {
            existingToken.RevokedAtUtc = nowUtc;
        }

        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.UserId,
            TokenHash = resetHash,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = expiresAtUtc,
        });

        await _db.SaveChangesAsync(ct);

        // Temporary response until email/RabbitMQ contract is added.
        return Ok(new ForgotPasswordResponse
        {
            ResetToken = resetToken,
            ExpiresAtUtc = expiresAtUtc,
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;
        var resetHash = TokenService.HashRefreshToken(request.ResetToken);

        var reset = await _db.PasswordResetTokens
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == resetHash, ct);

        if (reset is null ||
            reset.User is null ||
            !reset.User.IsActive ||
            reset.ExpiresAtUtc <= nowUtc ||
            reset.UsedAtUtc is not null ||
            reset.RevokedAtUtc is not null)
        {
            return Unauthorized();
        }

        reset.User.PasswordHash = _passwordHasher.HashPassword(reset.User, request.NewPassword);
        reset.UsedAtUtc = nowUtc;
        reset.RevokedAtUtc = nowUtc;

        var activeSessions = await _db.RefreshTokens
            .Where(x => x.UserId == reset.UserId && x.RevokedAtUtc == null && x.UsedAtUtc == null && x.ExpiresAtUtc > nowUtc)
            .ToListAsync(ct);

        foreach (var activeSession in activeSessions)
        {
            activeSession.RevokedAtUtc = nowUtc;
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

