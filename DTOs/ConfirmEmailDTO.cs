using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.DTOs;

public class ConfirmEmailDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Token { get; set; } = string.Empty;
}

