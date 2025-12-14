using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.DTOs;

public class ReviewDTO
{
    public Guid ReviewId { get; set; }

    public Guid BookId { get; set; }

    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime ReviewDate { get; set; }
}




