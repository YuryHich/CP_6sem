using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.DTOs;

public class LanguageDTO
{
    public Guid LanguageId { get; set; }
    
    [Required]
    public string LanguageName { get; set; } = string.Empty;
}


