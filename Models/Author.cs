namespace LibraryManagement.Models;

public class Author
{
    public Guid AuthorId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Country { get; set; }
    public string? Biography { get; set; }
}

