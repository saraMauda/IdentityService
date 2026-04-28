using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityService.Data.Entities;

public sealed class RefreshToken
{
    [Key]
    public long RefreshTokenId { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    [StringLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? UsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    [StringLength(64)]
    public string? ReplacedByTokenHash { get; set; }
}

