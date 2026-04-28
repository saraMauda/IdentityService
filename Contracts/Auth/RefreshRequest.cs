using System.ComponentModel.DataAnnotations;

namespace IdentityService.Contracts.Auth;

public sealed class RefreshRequest
{
    [Required]
    [MinLength(10)]
    public string RefreshToken { get; set; } = string.Empty;
}

