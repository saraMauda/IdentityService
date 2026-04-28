namespace IdentityService.Contracts.Auth;

public sealed class ForgotPasswordResponse
{
    public string ResetToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

