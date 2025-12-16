namespace LibraryManagement.Services;

public interface IExportService
{
    Task<byte[]> ExportDatabaseToCsvAsync();
    Task<byte[]> ExportDatabaseToXmlAsync();
}

