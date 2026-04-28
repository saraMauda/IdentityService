using System.ComponentModel.DataAnnotations;

namespace IdentityService.Data.Entities;

public class PreApprovedStudent
{
    [Key]
    public int PreApprovedId { get; set; }

    [Required]
    [StringLength(9)] 
    public string NationalId { get; set; } = string.Empty;

    public bool IsRegistered { get; set; } = false;
    
    public DateTime? RegistrationDate { get; set; } 
}