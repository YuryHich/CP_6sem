using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.DTOs;

public class PublisherDTO
{
    public Guid PublisherId { get; set; }
    
    [Required]
    public string PublisherName { get; set; } = string.Empty;
    public string? Country { get; set; }
}


