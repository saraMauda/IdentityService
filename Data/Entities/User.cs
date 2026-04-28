using System.ComponentModel.DataAnnotations;

namespace IdentityService.Data.Entities;

public class User
{
    [Key]
    public int UserId { get; set; }

    [Required]
    [StringLength(9)] 
    public string NationalId { get; set; } = string.Empty;

    [Required]
    [EmailAddress] 
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true; 

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
}