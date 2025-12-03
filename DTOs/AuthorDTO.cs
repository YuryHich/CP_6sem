using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.DTOs;

public class AuthorDTO
{
    public Guid AuthorId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    public DateTime? DateOfBirth { get; set; }
    
    [StringLength(50)]
    public string? Country { get; set; }
    
    public string? Biography { get; set; }
}

