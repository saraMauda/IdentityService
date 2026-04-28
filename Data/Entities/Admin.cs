using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityService.Data.Entities;

public class Admin
{
    [Key]
    public int AdminId { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!; 

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;
}
