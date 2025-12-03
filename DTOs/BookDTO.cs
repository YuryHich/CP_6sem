using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.DTOs;

public class BookDTO
{
    public Guid? BookId { get; set; }
    
    [Required]
    [StringLength(13)]
    public string Isbn { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int? PublicationYear { get; set; }
    
    public List<Guid> AuthorIds { get; set; } = new();
    
    public List<string> AuthorNames { get; set; } = new();
    
    public List<Guid> GenreIds { get; set; } = new();
    
    public List<string> GenreNames { get; set; } = new();
    
    public Guid? PublisherId { get; set; }
    
    public Guid? LanguageId { get; set; }
    
    public Guid? SeriesId { get; set; }
    
    public string? CoverImagePath { get; set; }
    
    public int AvailableCopies { get; set; }
    
    public int CopiesCount { get; set; } = 1;
    
    public List<BranchAvailabilityDTO> BranchAvailability { get; set; } = new();
}

