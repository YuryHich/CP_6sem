using LibraryManagement.DTOs;

namespace LibraryManagement.Services;

public interface IHistoryService
{
    Task<List<HistoryRecordDTO>> GetBooksHistoryAsync(DateTime? fromDate, DateTime? toDate, IReadOnlyCollection<string> operationTypes, string? usernameFilter);
    Task<List<HistoryRecordDTO>> GetLoansHistoryAsync(DateTime? fromDate, DateTime? toDate, IReadOnlyCollection<string> operationTypes, string? usernameFilter);
    Task<List<string>> GetUsernamesAsync();
}




