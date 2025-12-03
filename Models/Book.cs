namespace LibraryManagement.Models;

public class Book
{
    public Guid BookId { get; set; }
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? PublicationYear { get; set; }
    public Guid? PublisherId { get; set; }
    public Guid? LanguageId { get; set; }
    public Guid? SeriesId { get; set; }
    public string? CoverImagePath { get; set; }
}

