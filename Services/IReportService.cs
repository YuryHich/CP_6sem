namespace LibraryManagement.Services;

public interface IReportService
{
    Task<byte[]> GeneratePdfReportAsync(DateTime fromDate, DateTime toDate);
    Task<byte[]> GenerateExcelReportAsync(DateTime fromDate, DateTime toDate);
    Task<byte[]> GenerateLoansPdfReportAsync();
}

