using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.DTOs;

public class SeriesDTO
{
    public Guid SeriesId { get; set; }
    
    [Required]
    public string SeriesName { get; set; } = string.Empty;
    public string? Description { get; set; }
}


