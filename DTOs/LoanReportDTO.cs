namespace LibraryManagement.DTOs;

public class LoanReportDTO
{
    public string Username { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public bool IsOverdue { get; set; }
}

