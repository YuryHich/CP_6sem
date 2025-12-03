using Npgsql;
using LibraryManagement.DataAccess;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using ClosedXML.Excel;

namespace LibraryManagement.Services;

public class ReportService : IReportService
{
    private readonly DatabaseConnection _db;

    public ReportService(DatabaseConnection db)
    {
        _db = db;
    }

    public async Task<byte[]> GeneratePdfReportAsync(DateTime fromDate, DateTime toDate)
    {
        using var stream = new MemoryStream();
        using var writer = new PdfWriter(stream);
        using var pdf = new PdfDocument(writer);
        var document = new Document(pdf);

        document.Add(new Paragraph($"Отчет о библиотеке за период: {fromDate:dd.MM.yyyy} - {toDate:dd.MM.yyyy}")
            .SetFontSize(16)
            .SetBold());

        // Получаем статистику
        var stats = await GetStatisticsAsync(fromDate, toDate);
        var popularBooks = await GetPopularBooksAsync(fromDate, toDate);
        var fines = await GetFinesStatisticsAsync(fromDate, toDate);

        document.Add(new Paragraph($"Всего займов: {stats.TotalLoans}"));
        document.Add(new Paragraph($"Всего возвратов: {stats.TotalReturns}"));
        document.Add(new Paragraph($"Общая сумма штрафов: {fines.TotalAmount:F2} руб."));
        document.Add(new Paragraph($"Оплачено штрафов: {fines.PaidAmount:F2} руб."));

        document.Add(new Paragraph("\nПопулярные книги:").SetBold());
        var table = new Table(3);
        table.AddHeaderCell("Название");
        table.AddHeaderCell("ISBN");
        table.AddHeaderCell("Количество займов");

        foreach (var book in popularBooks)
        {
            table.AddCell(book.Title);
            table.AddCell(book.Isbn);
            table.AddCell(book.LoanCount.ToString());
        }

        document.Add(table);
        document.Close();

        return stream.ToArray();
    }

    public async Task<byte[]> GenerateExcelReportAsync(DateTime fromDate, DateTime toDate)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Отчет");

        worksheet.Cell(1, 1).Value = $"Отчет о библиотеке за период: {fromDate:dd.MM.yyyy} - {toDate:dd.MM.yyyy}";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;

        var stats = await GetStatisticsAsync(fromDate, toDate);
        var popularBooks = await GetPopularBooksAsync(fromDate, toDate);
        var fines = await GetFinesStatisticsAsync(fromDate, toDate);

        int row = 3;
        worksheet.Cell(row, 1).Value = "Всего займов:";
        worksheet.Cell(row, 2).Value = stats.TotalLoans;
        row++;

        worksheet.Cell(row, 1).Value = "Всего возвратов:";
        worksheet.Cell(row, 2).Value = stats.TotalReturns;
        row++;

        worksheet.Cell(row, 1).Value = "Общая сумма штрафов:";
        worksheet.Cell(row, 2).Value = fines.TotalAmount;
        row++;

        worksheet.Cell(row, 1).Value = "Оплачено штрафов:";
        worksheet.Cell(row, 2).Value = fines.PaidAmount;
        row += 2;

        worksheet.Cell(row, 1).Value = "Популярные книги:";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        row++;

        worksheet.Cell(row, 1).Value = "Название";
        worksheet.Cell(row, 2).Value = "ISBN";
        worksheet.Cell(row, 3).Value = "Количество займов";
        worksheet.Row(row).Style.Font.Bold = true;
        row++;

        foreach (var book in popularBooks)
        {
            worksheet.Cell(row, 1).Value = book.Title;
            worksheet.Cell(row, 2).Value = book.Isbn;
            worksheet.Cell(row, 3).Value = book.LoanCount;
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<(int TotalLoans, int TotalReturns)> GetStatisticsAsync(DateTime fromDate, DateTime toDate)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT 
                COUNT(*) FILTER (WHERE loan_date >= @fromDate AND loan_date <= @toDate) as total_loans,
                COUNT(*) FILTER (WHERE return_date >= @fromDate AND return_date <= @toDate) as total_returns
            FROM Loans";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@fromDate", fromDate);
        cmd.Parameters.AddWithValue("@toDate", toDate);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return (reader.GetInt32(0), reader.GetInt32(1));
        }

        return (0, 0);
    }

    private async Task<List<(string Title, string Isbn, int LoanCount)>> GetPopularBooksAsync(DateTime fromDate, DateTime toDate)
    {
        var books = new List<(string, string, int)>();
        
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT b.title, b.isbn, COUNT(l.loan_id) as loan_count
            FROM Books b
            INNER JOIN BookCopies bc ON b.book_id = bc.book_id
            INNER JOIN Loans l ON bc.copy_id = l.copy_id
            WHERE l.loan_date >= @fromDate AND l.loan_date <= @toDate
            GROUP BY b.book_id, b.title, b.isbn
            ORDER BY loan_count DESC
            LIMIT 10";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@fromDate", fromDate);
        cmd.Parameters.AddWithValue("@toDate", toDate);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            books.Add((reader.GetString(0), reader.GetString(1), reader.GetInt32(2)));
        }

        return books;
    }

    private async Task<(decimal TotalAmount, decimal PaidAmount)> GetFinesStatisticsAsync(DateTime fromDate, DateTime toDate)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT 
                COALESCE(SUM(amount), 0) as total_amount,
                COALESCE(SUM(amount) FILTER (WHERE paid = true), 0) as paid_amount
            FROM Fines
            WHERE fine_date >= @fromDate AND fine_date <= @toDate";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@fromDate", fromDate);
        cmd.Parameters.AddWithValue("@toDate", toDate);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return (reader.GetDecimal(0), reader.GetDecimal(1));
        }

        return (0, 0);
    }
}

