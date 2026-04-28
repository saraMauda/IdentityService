using System.ComponentModel.DataAnnotations;

namespace IdentityService.Contracts.Auth;

public sealed class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

