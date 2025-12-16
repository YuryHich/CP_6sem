using System.Text;
using System.Xml;
using CsvHelper;
using System.Globalization;
using LibraryManagement.DataAccess;
using Npgsql;

namespace LibraryManagement.Services;

public class ExportService : IExportService
{
    private readonly DatabaseConnection _db;

    public ExportService(DatabaseConnection db)
    {
        _db = db;
    }

    public async Task<byte[]> ExportDatabaseToCsvAsync()
    {
        var csvContent = new StringBuilder();

        // Экспортируем каждую таблицу (полностью, включая пароли и токены)
        await ExportTableAsync("Books", csvContent, new[] { "book_id", "isbn", "title", "description", "publication_year", "publisher_id", "language_id", "series_id", "cover_image_path", "default_copy_count" });
        await ExportTableAsync("Users", csvContent, new[] { "user_id", "username", "email", "password_hash", "first_name", "last_name", "date_of_birth", "registration_date", "role_id", "is_active", "confirmation_token", "is_email_confirmed", "password_reset_token", "reset_token_expiration" });
        await ExportTableAsync("Loans", csvContent, new[] { "loan_id", "copy_id", "user_id", "loan_date", "due_date", "return_date" });
        await ExportTableAsync("BookCopies", csvContent, new[] { "copy_id", "book_id", "branch_id", "status", "acquisition_date" });
        await ExportTableAsync("Reviews", csvContent, new[] { "review_id", "book_id", "user_id", "rating", "comment", "review_date" });
        await ExportTableAsync("Authors", csvContent, new[] { "author_id", "first_name", "last_name", "date_of_birth", "country", "biography" });
        await ExportTableAsync("Genres", csvContent, new[] { "genre_id", "genre_name" });
        await ExportTableAsync("Publishers", csvContent, new[] { "publisher_id", "publisher_name", "country" });
        await ExportTableAsync("Languages", csvContent, new[] { "language_id", "language_name" });
        await ExportTableAsync("Branches", csvContent, new[] { "branch_id", "branch_name", "address", "city" });

        // Добавляем BOM для корректного отображения кириллицы в Excel
        var bom = Encoding.UTF8.GetPreamble();
        var contentBytes = Encoding.UTF8.GetBytes(csvContent.ToString());
        var result = new byte[bom.Length + contentBytes.Length];
        Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
        Buffer.BlockCopy(contentBytes, 0, result, bom.Length, contentBytes.Length);

        return result;
    }

    private async Task ExportTableAsync(string tableName, StringBuilder csvContent, string[] columns, string[]? excludeColumns = null)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        // Формируем список колонок для SELECT (экспортируем все колонки)
        var selectColumns = columns;
        var columnList = string.Join(", ", selectColumns.Select(c => $"\"{c}\""));

        var sql = $"SELECT {columnList} FROM {tableName} ORDER BY 1";

        using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        // Заголовок таблицы
        csvContent.AppendLine($"=== {tableName} ===");
        csvContent.AppendLine(string.Join(",", selectColumns.Select(c => EscapeCsvValue(c))));

        // Данные
        while (await reader.ReadAsync())
        {
            var row = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.IsDBNull(i))
                {
                    row.Add("");
                }
                else
                {
                    var value = reader.GetValue(i).ToString() ?? "";
                    row.Add(EscapeCsvValue(value));
                }
            }
            csvContent.AppendLine(string.Join(",", row));
        }

        csvContent.AppendLine(); // Пустая строка между таблицами
    }

    private string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        // Если значение содержит запятую, кавычки или перенос строки, оборачиваем в кавычки
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            // Экранируем кавычки удвоением
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }

        return value;
    }

    public async Task<byte[]> ExportDatabaseToXmlAsync()
    {
        using var memoryStream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = Encoding.UTF8,
            Async = true
        };

        using var writer = XmlWriter.Create(memoryStream, settings);
        
        await writer.WriteStartDocumentAsync();
        await writer.WriteStartElementAsync(null, "Database", null);
        await writer.WriteAttributeStringAsync(null, "exportDate", null, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        // Экспортируем каждую таблицу (полностью, включая пароли и токены)
        await ExportTableToXmlAsync("Books", writer, new[] { "book_id", "isbn", "title", "description", "publication_year", "publisher_id", "language_id", "series_id", "cover_image_path", "default_copy_count" });
        await ExportTableToXmlAsync("Users", writer, new[] { "user_id", "username", "email", "password_hash", "first_name", "last_name", "date_of_birth", "registration_date", "role_id", "is_active", "confirmation_token", "is_email_confirmed", "password_reset_token", "reset_token_expiration" });
        await ExportTableToXmlAsync("Loans", writer, new[] { "loan_id", "copy_id", "user_id", "loan_date", "due_date", "return_date" });
        await ExportTableToXmlAsync("BookCopies", writer, new[] { "copy_id", "book_id", "branch_id", "status", "acquisition_date" });
        await ExportTableToXmlAsync("Reviews", writer, new[] { "review_id", "book_id", "user_id", "rating", "comment", "review_date" });
        await ExportTableToXmlAsync("Authors", writer, new[] { "author_id", "first_name", "last_name", "date_of_birth", "country", "biography" });
        await ExportTableToXmlAsync("Genres", writer, new[] { "genre_id", "genre_name" });
        await ExportTableToXmlAsync("Publishers", writer, new[] { "publisher_id", "publisher_name", "country" });
        await ExportTableToXmlAsync("Languages", writer, new[] { "language_id", "language_name" });
        await ExportTableToXmlAsync("Branches", writer, new[] { "branch_id", "branch_name", "address", "city" });

        await writer.WriteEndElementAsync(); // Database
        await writer.WriteEndDocumentAsync();
        await writer.FlushAsync();

        return memoryStream.ToArray();
    }

    private async Task ExportTableToXmlAsync(string tableName, XmlWriter writer, string[] columns, string[]? excludeColumns = null)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        // Экспортируем все колонки
        var selectColumns = columns;
        var columnList = string.Join(", ", selectColumns.Select(c => $"\"{c}\""));

        var sql = $"SELECT {columnList} FROM {tableName} ORDER BY 1";

        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        await writer.WriteStartElementAsync(null, "Table", null);
        await writer.WriteAttributeStringAsync(null, "name", null, tableName);

        while (await reader.ReadAsync())
        {
            await writer.WriteStartElementAsync(null, "Row", null);
            
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = selectColumns[i];
                await writer.WriteStartElementAsync(null, columnName, null);
                
                if (reader.IsDBNull(i))
                {
                    await writer.WriteAttributeStringAsync(null, "null", null, "true");
                }
                else
                {
                    var value = reader.GetValue(i);
                    var stringValue = value?.ToString() ?? "";
                    // Экранируем XML-специальные символы
                    await writer.WriteStringAsync(stringValue);
                }
                
                await writer.WriteEndElementAsync();
            }
            
            await writer.WriteEndElementAsync(); // Row
        }

        await writer.WriteEndElementAsync(); // Table
    }
}

