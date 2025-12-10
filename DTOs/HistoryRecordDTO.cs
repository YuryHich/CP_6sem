namespace LibraryManagement.DTOs;

public class HistoryRecordDTO
{
    public Guid HistoryId { get; set; }
    public DateTime OperationDate { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string Username { get; set; } = string.Empty;
}




