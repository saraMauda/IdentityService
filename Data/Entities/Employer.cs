using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityService.Data.Entities;

public class Employer
{
    [Key]
    public int EmployerId { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!; 

    [Required]
    [StringLength(100)]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [Phone] 
    [StringLength(15)]
    public string ContactPhone { get; set; } = string.Empty;
}