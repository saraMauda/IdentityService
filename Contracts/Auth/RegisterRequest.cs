using IdentityService.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace IdentityService.Contracts.Auth;

public sealed class RegisterRequest
{
    [Required]
    [StringLength(9, MinimumLength = 9)]
    public string NationalId { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Student;
}

