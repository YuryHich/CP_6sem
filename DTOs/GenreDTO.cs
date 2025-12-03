using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.DTOs;

public class GenreDTO
{
    public Guid GenreId { get; set; }
    
    [Required]
    public string GenreName { get; set; } = string.Empty;
}


