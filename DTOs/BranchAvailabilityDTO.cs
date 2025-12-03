namespace LibraryManagement.DTOs;

public class BranchAvailabilityDTO
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
}





