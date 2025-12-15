using Npgsql;
using LibraryManagement.DataAccess;
using LibraryManagement.DTOs;
using iText.Kernel.Pdf;
using iText.Kernel.Colors;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
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
        var stream = new MemoryStream();
        PdfWriter? writer = null;
        PdfDocument? pdf = null;
        Document? document = null;

        try
        {
            writer = new PdfWriter(stream);
            pdf = new PdfDocument(writer);
            document = new Document(pdf);

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
            pdf.Close();
            writer.Close();

            return stream.ToArray();
        }
        catch (Exception ex)
        {
            document?.Close();
            pdf?.Close();
            writer?.Close();
            stream?.Dispose();
            throw new Exception($"Ошибка при генерации PDF: {ex.Message}", ex);
        }
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

    public async Task<byte[]> GenerateLoansPdfReportAsync()
    {
        var stream = new MemoryStream();
        PdfWriter? writer = null;
        PdfDocument? pdf = null;
        Document? document = null;

        try
        {
            writer = new PdfWriter(stream);
            pdf = new PdfDocument(writer);
            document = new Document(pdf);

            // Заголовок
            var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var title = new Paragraph("Отчёт о взятых книгах")
                .SetFont(titleFont)
                .SetFontSize(14)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20);
            document.Add(title);

            // Получаем данные о займах
            var loans = await GetLoansReportDataAsync();

            if (loans.Count == 0)
            {
                document.Add(new Paragraph("Нет данных о займах").SetFontSize(12));
                document.Close();
                pdf.Close();
                writer.Close();
                return stream.ToArray();
            }

            // Создаём таблицу
            var table = new Table(5, true);
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Заголовки таблицы
            var headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            table.AddHeaderCell(new Cell().Add(new Paragraph("Пользователь").SetFont(headerFont)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Книга").SetFont(headerFont)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Дата выдачи").SetFont(headerFont)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Срок возврата").SetFont(headerFont)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Статус").SetFont(headerFont)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));

            // Данные
            var bodyFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            bool isEvenRow = false;
            
            foreach (var loan in loans)
            {
                var rowColor = isEvenRow ? ColorConstants.LIGHT_GRAY : ColorConstants.WHITE;
                isEvenRow = !isEvenRow;

                table.AddCell(new Cell().Add(new Paragraph(loan.Username).SetFont(bodyFont)).SetBackgroundColor(rowColor));
                table.AddCell(new Cell().Add(new Paragraph(loan.BookTitle).SetFont(bodyFont)).SetBackgroundColor(rowColor));
                table.AddCell(new Cell().Add(new Paragraph(loan.LoanDate.ToString("dd.MM.yyyy")).SetFont(bodyFont)).SetBackgroundColor(rowColor));
                table.AddCell(new Cell().Add(new Paragraph(loan.DueDate.ToString("dd.MM.yyyy")).SetFont(bodyFont)).SetBackgroundColor(rowColor));
                
                string statusText;
                if (loan.ReturnDate.HasValue)
                {
                    statusText = $"Возвращена {loan.ReturnDate.Value:dd.MM.yyyy}";
                }
                else if (loan.IsOverdue)
                {
                    statusText = "Просрочена";
                }
                else
                {
                    statusText = "В займе";
                }
                
                var statusColor = loan.IsOverdue ? ColorConstants.RED : (loan.ReturnDate.HasValue ? ColorConstants.GREEN : ColorConstants.BLACK);
                table.AddCell(new Cell().Add(new Paragraph(statusText).SetFont(bodyFont).SetFontColor(statusColor)).SetBackgroundColor(rowColor));
            }

            document.Add(table);

            // Подвал с датой и страницей
            var footer = new Paragraph($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}")
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetMarginTop(20);
            document.Add(footer);

            document.Close();
            pdf.Close();
            writer.Close();
            
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            document?.Close();
            pdf?.Close();
            writer?.Close();
            stream?.Dispose();
            throw new Exception($"Ошибка при генерации PDF: {ex.Message}", ex);
        }
    }

    private async Task<List<LoanReportDTO>> GetLoansReportDataAsync()
    {
        var loans = new List<LoanReportDTO>();
        
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT 
                u.username,
                b.title AS book_title,
                l.loan_date,
                l.due_date,
                l.return_date,
                CASE 
                    WHEN l.return_date IS NULL AND l.due_date < CURRENT_TIMESTAMP THEN true
                    ELSE false
                END AS is_overdue
            FROM Loans l
            INNER JOIN Users u ON l.user_id = u.user_id
            INNER JOIN BookCopies bc ON l.copy_id = bc.copy_id
            INNER JOIN Books b ON bc.book_id = b.book_id
            ORDER BY l.loan_date DESC";

        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            loans.Add(new LoanReportDTO
            {
                Username = reader.GetString(0),
                BookTitle = reader.GetString(1),
                LoanDate = reader.GetDateTime(2),
                DueDate = reader.GetDateTime(3),
                ReturnDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                IsOverdue = reader.GetBoolean(5)
            });
        }

        return loans;
    }
}

