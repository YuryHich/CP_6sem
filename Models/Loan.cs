namespace LibraryManagement.Models;

public class Loan
{
    public Guid LoanId { get; set; }
    public Guid CopyId { get; set; }
    public Guid UserId { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
}

