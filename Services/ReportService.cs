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
using iText.Kernel.Geom;
using iText.Layout.Borders;
using iText.Kernel.Pdf.Canvas.Draw;
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
        PdfWriter? writer = null;
        PdfDocument? pdf = null;
        Document? document = null;
        
        try
        {
            var stream = new MemoryStream();
            
            // Создаём PdfWriter БЕЗ WriterProperties
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
            
            var result = stream.ToArray();
            stream.Dispose();
            return result;
        }
        catch (Exception ex)
        {
            document?.Close();
            throw new Exception($"Ошибка при генерации PDF: {ex.Message}. StackTrace: {ex.StackTrace}", ex);
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
        try
        {
            // Получаем данные о займах и статистику
            var loans = await GetLoansReportDataAsync();
            var globalStats = await GetGlobalStatsForPdfAsync();

            var stream = new MemoryStream();
            
            // Создаём PdfWriter
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);
            document.SetMargins(40, 40, 40, 40);

            // === ТИТУЛЬНАЯ СТРАНИЦА ===
            var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            
            // Заголовок
            var title = new Paragraph("АНАЛИТИЧЕСКИЙ ОТЧЁТ")
                .SetFont(titleFont)
                .SetFontSize(26)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(80)
                .SetMarginBottom(10);
            document.Add(title);

            var subtitle = new Paragraph("О КНИЖНЫХ ЗАЙМАХ И ДЕЯТЕЛЬНОСТИ БИБЛИОТЕКИ")
                .SetFont(titleFont)
                .SetFontSize(18)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(40)
                .SetFontColor(new DeviceRgb(52, 73, 94));
            document.Add(subtitle);

            // Дата
            var dateBox = new Paragraph($"Дата формирования отчёта: {DateTime.Now:dd MMMM yyyy г.}\nВремя: {DateTime.Now:HH:mm}")
                .SetFont(regularFont)
                .SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(60)
                .SetFontColor(ColorConstants.DARK_GRAY);
            document.Add(dateBox);

            // Линия-разделитель
            var lineSeparator = new LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.SolidLine(2));
            lineSeparator.SetMarginTop(10).SetMarginBottom(20);
            document.Add(lineSeparator);

            // === ВСТУПИТЕЛЬНОЕ СЛОВО ===
            var greeting = new Paragraph("Уважаемые коллеги!")
                .SetFont(titleFont)
                .SetFontSize(14)
                .SetMarginBottom(15);
            document.Add(greeting);

            var introText = new Paragraph(
                "Представляем вашему вниманию аналитический отчёт о работе библиотечной системы. " +
                "Данный документ содержит комплексный анализ книжных займов, статистику использования фонда " +
                "и ключевые показатели эффективности работы библиотеки.\n\n" +
                "Цель отчёта:\n" +
                "• Предоставить полную картину деятельности библиотеки\n" +
                "• Выявить тенденции и закономерности в использовании литературы\n" +
                "• Помочь в принятии управленческих решений\n" +
                "• Обеспечить контроль за своевременностью возврата книг"
            )
                .SetFont(regularFont)
                .SetFontSize(11)
                .SetTextAlignment(TextAlignment.JUSTIFIED)
                .SetMarginBottom(30);
            document.Add(introText);

            // Новая страница для статистики
            document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

            // === РАЗДЕЛ 1: ОБЩАЯ СТАТИСТИКА ===
            var statsHeader = new Paragraph("РАЗДЕЛ 1. ОБЩАЯ СТАТИСТИКА БИБЛИОТЕКИ")
                .SetFont(titleFont)
                .SetFontSize(16)
                .SetMarginBottom(20)
                .SetFontColor(new DeviceRgb(41, 128, 185));
            document.Add(statsHeader);

            // Цветные карточки статистики в таблице 3x2
            var statsTable = new Table(new float[] { 1, 1, 1 });
            statsTable.SetWidth(UnitValue.CreatePercentValue(100));
            statsTable.SetMarginBottom(30);

            AddColoredStatsCell(statsTable, "📚 Всего книг", globalStats.TotalBooks.ToString(), new DeviceRgb(52, 152, 219));
            AddColoredStatsCell(statsTable, "📖 Всего займов", globalStats.TotalLoans.ToString(), new DeviceRgb(46, 204, 113));
            AddColoredStatsCell(statsTable, "⏱ Активных займов", globalStats.ActiveLoans.ToString(), new DeviceRgb(155, 89, 182));
            AddColoredStatsCell(statsTable, "⚠ Просроченных", globalStats.OverdueLoans.ToString(), new DeviceRgb(231, 76, 60));
            AddColoredStatsCell(statsTable, "👥 Пользователей", globalStats.TotalUsers.ToString(), new DeviceRgb(52, 73, 94));
            AddColoredStatsCell(statsTable, "✍ Авторов", globalStats.TotalAuthors.ToString(), new DeviceRgb(230, 126, 34));

            document.Add(statsTable);

            // === ПРОЦЕНТНЫЙ АНАЛИЗ ===
            var percentTitle = new Paragraph("Процентный анализ")
                .SetFont(titleFont)
                .SetFontSize(14)
                .SetMarginTop(10)
                .SetMarginBottom(15);
            document.Add(percentTitle);

            // Расчёт процентов
            var returnedCount = loans.Count(l => l.ReturnDate.HasValue);
            var activePercent = globalStats.TotalLoans > 0 ? (double)globalStats.ActiveLoans / globalStats.TotalLoans * 100 : 0;
            var overduePercent = globalStats.ActiveLoans > 0 ? (double)globalStats.OverdueLoans / globalStats.ActiveLoans * 100 : 0;
            var returnedPercent = globalStats.TotalLoans > 0 ? (double)returnedCount / globalStats.TotalLoans * 100 : 0;

            // Таблица процентов с визуальными полосами
            var percentTable = new Table(new float[] { 3, 1, 4 });
            percentTable.SetWidth(UnitValue.CreatePercentValue(100));
            percentTable.SetMarginBottom(30);

            AddPercentRow(percentTable, "Активные займы", activePercent, new DeviceRgb(155, 89, 182));
            AddPercentRow(percentTable, "Просроченные займы", overduePercent, new DeviceRgb(231, 76, 60));
            AddPercentRow(percentTable, "Возвращённые книги", returnedPercent, new DeviceRgb(46, 204, 113));

            document.Add(percentTable);

            // Текстовый анализ
            var analysisText = new Paragraph(
                $"Анализ показателей:\n\n" +
                $"В настоящее время из {globalStats.TotalLoans} зарегистрированных займов {globalStats.ActiveLoans} " +
                $"({activePercent:F1}%) находятся в активном статусе. " +
                (globalStats.OverdueLoans > 0 
                    ? $"Обращаем внимание на {globalStats.OverdueLoans} просроченных займов ({overduePercent:F1}% от активных), " +
                      "что требует оперативного реагирования со стороны администрации. " 
                    : "Просроченных займов не зафиксировано, что свидетельствует о высокой дисциплине читателей. ") +
                $"Процент возврата книг составляет {returnedPercent:F1}%, что является " +
                (returnedPercent >= 70 ? "хорошим показателем." : "показателем, требующим внимания.")
            )
                .SetFont(regularFont)
                .SetFontSize(11)
                .SetTextAlignment(TextAlignment.JUSTIFIED)
                .SetMarginBottom(20)
                .SetBackgroundColor(new DeviceRgb(236, 240, 241))
                .SetPadding(15);
            document.Add(analysisText);

            // Новая страница для детального отчёта
            document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

            // === РАЗДЕЛ 2: ДЕТАЛЬНАЯ ИНФОРМАЦИЯ О ЗАЙМАХ ===
            var loansHeader = new Paragraph("РАЗДЕЛ 2. ДЕТАЛЬНАЯ ИНФОРМАЦИЯ О ЗАЙМАХ")
                .SetFont(titleFont)
                .SetFontSize(16)
                .SetMarginBottom(20)
                .SetFontColor(new DeviceRgb(41, 128, 185));
            document.Add(loansHeader);

            if (loans.Count == 0)
            {
                document.Add(new Paragraph("На данный момент займов не зарегистрировано.")
                    .SetFont(regularFont)
                    .SetFontSize(12)
                    .SetFontColor(ColorConstants.GRAY)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(50));
            }
            else
            {
                // Легенда
                var legend = new Paragraph("Легенда статусов:")
                    .SetFont(titleFont)
                    .SetFontSize(11)
                    .SetMarginBottom(10);
                document.Add(legend);

                var legendTable = new Table(new float[] { 1, 1, 1 });
                legendTable.SetWidth(UnitValue.CreatePercentValue(60));
                legendTable.SetMarginBottom(15);
                
                legendTable.AddCell(new Cell().Add(new Paragraph("✓ Возвращена").SetFontColor(new DeviceRgb(39, 174, 96)).SetBold()).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                legendTable.AddCell(new Cell().Add(new Paragraph("⚠ Просрочена").SetFontColor(new DeviceRgb(231, 76, 60)).SetBold()).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                legendTable.AddCell(new Cell().Add(new Paragraph("◐ Активна").SetFontColor(new DeviceRgb(52, 152, 219)).SetBold()).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                
                document.Add(legendTable);

                // Создаём таблицу займов
                var loansTable = new Table(new float[] { 2, 3, 1.5f, 1.5f, 1.5f });
                loansTable.SetWidth(UnitValue.CreatePercentValue(100));
                loansTable.SetFontSize(9);
                loansTable.SetMarginTop(10);

                // Заголовки таблицы
                var headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var headerCells = new string[] { "Пользователь", "Книга", "Дата выдачи", "Срок возврата", "Статус" };
                
                foreach (var header in headerCells)
                {
                    var cell = new Cell().Add(new Paragraph(header).SetFont(headerFont).SetFontSize(10))
                        .SetBackgroundColor(new DeviceRgb(52, 73, 94))
                        .SetFontColor(ColorConstants.WHITE)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetPadding(8)
                        .SetBold();
                    loansTable.AddHeaderCell(cell);
                }

                // Данные
                var bodyFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                bool isEvenRow = false;
                
                foreach (var loan in loans)
                {
                    var rowColor = isEvenRow ? new DeviceRgb(236, 240, 241) : ColorConstants.WHITE;
                    isEvenRow = !isEvenRow;

                    // Пользователь
                    loansTable.AddCell(new Cell().Add(new Paragraph(loan.Username).SetFont(bodyFont))
                        .SetBackgroundColor(rowColor)
                        .SetPadding(6));

                    // Книга
                    loansTable.AddCell(new Cell().Add(new Paragraph(loan.BookTitle).SetFont(bodyFont).SetFontSize(8))
                        .SetBackgroundColor(rowColor)
                        .SetPadding(6));

                    // Дата выдачи
                    loansTable.AddCell(new Cell().Add(new Paragraph(loan.LoanDate.ToString("dd.MM.yyyy")).SetFont(bodyFont))
                        .SetBackgroundColor(rowColor)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetPadding(6));

                    // Срок возврата
                    loansTable.AddCell(new Cell().Add(new Paragraph(loan.DueDate.ToString("dd.MM.yyyy")).SetFont(bodyFont))
                        .SetBackgroundColor(rowColor)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetPadding(6));

                    // Статус
                    string statusText;
                    DeviceRgb statusColor;
                    DeviceRgb statusBg;
                    
                    if (loan.ReturnDate.HasValue)
                    {
                        statusText = "✓ Возвращена";
                        statusColor = new DeviceRgb(39, 174, 96);
                        statusBg = new DeviceRgb(212, 239, 223);
                    }
                    else if (loan.IsOverdue)
                    {
                        statusText = "⚠ Просрочена";
                        statusColor = new DeviceRgb(231, 76, 60);
                        statusBg = new DeviceRgb(248, 215, 218);
                    }
                    else
                    {
                        statusText = "◐ Активна";
                        statusColor = new DeviceRgb(52, 152, 219);
                        statusBg = new DeviceRgb(209, 236, 241);
                    }
                    
                    loansTable.AddCell(new Cell().Add(new Paragraph(statusText).SetFont(bodyFont).SetFontColor(statusColor).SetBold())
                        .SetBackgroundColor(statusBg)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetPadding(6));
                }

                document.Add(loansTable);
            }

            // Новая страница для заключения
            document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

            // === ЗАКЛЮЧЕНИЕ И РЕКОМЕНДАЦИИ ===
            var conclusionHeader = new Paragraph("ЗАКЛЮЧЕНИЕ И РЕКОМЕНДАЦИИ")
                .SetFont(titleFont)
                .SetFontSize(16)
                .SetMarginTop(30)
                .SetMarginBottom(20)
                .SetFontColor(new DeviceRgb(41, 128, 185));
            document.Add(conclusionHeader);

            var conclusionText = new Paragraph(
                $"Итоги анализа деятельности библиотеки:\n\n" +
                $"По состоянию на {DateTime.Now:dd MMMM yyyy года} библиотечная система обслуживает " +
                $"{globalStats.TotalUsers} пользователей и располагает фондом из {globalStats.TotalBooks} книг " +
                $"{globalStats.TotalAuthors} авторов. За период работы зарегистрировано {globalStats.TotalLoans} займов.\n\n"
            )
                .SetFont(regularFont)
                .SetFontSize(11)
                .SetTextAlignment(TextAlignment.JUSTIFIED);
            document.Add(conclusionText);

            // Выводы в рамке
            var conclusions = new Paragraph(
                "КЛЮЧЕВЫЕ ВЫВОДЫ:\n\n" +
                $"✓ Активность пользователей: {(activePercent > 30 ? "Высокая" : activePercent > 15 ? "Средняя" : "Низкая")} " +
                $"({globalStats.ActiveLoans} активных займов)\n\n" +
                $"✓ Дисциплина возврата: {(overduePercent < 10 ? "Отличная" : overduePercent < 20 ? "Хорошая" : "Требует внимания")} " +
                $"({overduePercent:F1}% просрочек)\n\n" +
                $"✓ Популярность библиотеки: {(globalStats.TotalLoans / (double)globalStats.TotalUsers > 2 ? "Высокая" : "Средняя")} " +
                $"(в среднем {globalStats.TotalLoans / (double)Math.Max(globalStats.TotalUsers, 1):F1} займов на пользователя)"
            )
                .SetFont(regularFont)
                .SetFontSize(11)
                .SetBackgroundColor(new DeviceRgb(212, 239, 223))
                .SetPadding(15)
                .SetMarginTop(15)
                .SetMarginBottom(20)
                .SetBorder(new iText.Layout.Borders.SolidBorder(new DeviceRgb(39, 174, 96), 2));
            document.Add(conclusions);

            // Рекомендации
            var recommendations = new Paragraph("РЕКОМЕНДАЦИИ:")
                .SetFont(titleFont)
                .SetFontSize(12)
                .SetMarginTop(20)
                .SetMarginBottom(10);
            document.Add(recommendations);

            var recList = new Paragraph(
                "1. Проводить регулярный мониторинг просроченных займов и своевременно информировать читателей\n\n" +
                "2. Анализировать популярность изданий для оптимизации закупок новой литературы\n\n" +
                "3. Поощрять активных пользователей для повышения общей вовлечённости\n\n" +
                "4. Использовать автоматические напоминания для снижения количества просрочек\n\n" +
                "5. Регулярно генерировать отчёты для контроля показателей эффективности"
            )
                .SetFont(regularFont)
                .SetFontSize(11)
                .SetMarginBottom(30);
            document.Add(recList);

            document.Add(lineSeparator);

            // Завершающая информация
            var finalNote = new Paragraph(
                "Данный отчёт сформирован автоматически системой управления библиотекой.\n" +
                "Для получения дополнительной информации обращайтесь к администратору системы."
            )
                .SetFont(regularFont)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(ColorConstants.GRAY)
                .SetMarginTop(20);
            document.Add(finalNote);

            // Подпись
            var signature = new Paragraph($"Отчёт сформирован: {DateTime.Now:dd.MM.yyyy HH:mm} | Страниц: {pdf.GetNumberOfPages()}")
                .SetFont(regularFont)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(10)
                .SetFontColor(ColorConstants.GRAY);
            document.Add(signature);

            // Закрываем документ
            document.Close();
            
            var result = stream.ToArray();
            stream.Dispose();
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при генерации PDF отчёта о займах: {ex.Message}. StackTrace: {ex.StackTrace}", ex);
        }
    }

    // Вспомогательный метод для цветных карточек статистики
    private void AddColoredStatsCell(Table table, string label, string value, DeviceRgb color)
    {
        var cell = new Cell();
        
        // Эмодзи и название
        var labelPara = new Paragraph(label)
            .SetFontSize(10)
            .SetFontColor(ColorConstants.WHITE)
            .SetBold()
            .SetTextAlignment(TextAlignment.CENTER);
        
        // Значение крупно
        var valuePara = new Paragraph(value)
            .SetFontSize(24)
            .SetBold()
            .SetFontColor(ColorConstants.WHITE)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(5);
        
        cell.Add(labelPara);
        cell.Add(valuePara);
        cell.SetBackgroundColor(color);
        cell.SetPadding(15);
        cell.SetTextAlignment(TextAlignment.CENTER);
        cell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
        
        table.AddCell(cell);
    }

    // Метод для добавления процентных строк с визуальными полосами
    private void AddPercentRow(Table table, string label, double percent, DeviceRgb color)
    {
        // Название
        table.AddCell(new Cell().Add(new Paragraph(label).SetFontSize(11).SetBold())
            .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
            .SetPadding(8));
        
        // Процент
        table.AddCell(new Cell().Add(new Paragraph($"{percent:F1}%").SetFontSize(11).SetBold().SetFontColor(color))
            .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
            .SetTextAlignment(TextAlignment.RIGHT)
            .SetPadding(8));
        
        // Визуальная полоса
        var barWidth = (float)(percent / 100.0);
        var barCell = new Cell();
        
        // Создаём внутреннюю таблицу для полосы
        var barTable = new Table(new float[] { Math.Max(barWidth, 0.01f), Math.Max(1 - barWidth, 0.01f) });
        barTable.SetWidth(UnitValue.CreatePercentValue(100));
        
        barTable.AddCell(new Cell().SetBackgroundColor(color).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetHeight(20));
        barTable.AddCell(new Cell().SetBackgroundColor(new DeviceRgb(236, 240, 241)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetHeight(20));
        
        barCell.Add(barTable);
        barCell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
        barCell.SetPadding(8);
        
        table.AddCell(barCell);
    }

    // Метод для получения глобальной статистики
    private async Task<GlobalStatsDTO> GetGlobalStatsForPdfAsync()
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var stats = new GlobalStatsDTO();

        // Общее количество книг
        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM Books", conn))
        {
            stats.TotalBooks = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // Общее количество займов
        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM Loans", conn))
        {
            stats.TotalLoans = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // Активные займы
        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM Loans WHERE return_date IS NULL", conn))
        {
            stats.ActiveLoans = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // Просроченные займы
        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM Loans WHERE return_date IS NULL AND due_date < CURRENT_DATE", conn))
        {
            stats.OverdueLoans = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // Пользователи
        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM Users", conn))
        {
            stats.TotalUsers = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // Авторы
        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM Authors", conn))
        {
            stats.TotalAuthors = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        return stats;
    }

    // DTO для глобальной статистики
    private class GlobalStatsDTO
    {
        public int TotalBooks { get; set; }
        public int TotalLoans { get; set; }
        public int ActiveLoans { get; set; }
        public int OverdueLoans { get; set; }
        public int TotalUsers { get; set; }
        public int TotalAuthors { get; set; }
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

