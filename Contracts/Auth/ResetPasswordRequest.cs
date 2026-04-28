using System.ComponentModel.DataAnnotations;

namespace IdentityService.Contracts.Auth;

public sealed class ResetPasswordRequest
{
    [Required]
    [MinLength(10)]
    public string ResetToken { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}

