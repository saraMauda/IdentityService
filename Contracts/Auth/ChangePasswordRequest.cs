using System.ComponentModel.DataAnnotations;

namespace IdentityService.Contracts.Auth;

public sealed class ChangePasswordRequest
{
    [Required]
    [MinLength(10)]
    public string RefreshToken { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}

