using System.ComponentModel.DataAnnotations;

namespace IdentityService.Contracts.Auth;

public sealed class LoginRequest
{
    [Required]
    [StringLength(9, MinimumLength = 9)]
    public string NationalId { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string Password { get; set; } = string.Empty;
}

