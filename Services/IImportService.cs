namespace LibraryManagement.Services;

public interface IImportService
{
    Task<(bool Success, string Message)> ImportFromCsvAsync(Stream fileStream);
    Task<(bool Success, string Message)> ImportFromXmlAsync(Stream fileStream);
}

