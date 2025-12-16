using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.DTOs;

public class ForgotPasswordDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

