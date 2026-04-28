namespace IdentityService.Contracts.Auth;

public sealed class MeResponse
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

