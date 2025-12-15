using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.DTOs;

public class DeleteAccountDTO
{
    [Required]
    public string Password { get; set; } = string.Empty;
}

