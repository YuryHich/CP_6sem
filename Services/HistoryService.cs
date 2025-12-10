using LibraryManagement.DataAccess;
using LibraryManagement.DTOs;

namespace LibraryManagement.Services;

public class HistoryService : IHistoryService
{
    private readonly HistoryRepository _repository;

    public HistoryService(DatabaseConnection db)
    {
        _repository = new HistoryRepository(db);
    }

    public Task<List<HistoryRecordDTO>> GetBooksHistoryAsync(DateTime? fromDate, DateTime? toDate, IReadOnlyCollection<string> operationTypes, string? usernameFilter)
    {
        return _repository.GetBooksHistoryAsync(fromDate, toDate, operationTypes, usernameFilter);
    }

    public Task<List<HistoryRecordDTO>> GetLoansHistoryAsync(DateTime? fromDate, DateTime? toDate, IReadOnlyCollection<string> operationTypes, string? usernameFilter)
    {
        return _repository.GetLoansHistoryAsync(fromDate, toDate, operationTypes, usernameFilter);
    }

    public Task<List<string>> GetUsernamesAsync()
    {
        return _repository.GetDistinctUsernamesAsync();
    }
}




