using System.Data;
using System.Text;
using System.Xml;
using CsvHelper;
using System.Globalization;
using LibraryManagement.DataAccess;
using Npgsql;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Services;

public class ImportService : IImportService
{
    private readonly DatabaseConnection _db;
    private readonly ILogger<ImportService>? _logger;

    // Определяем структуру таблиц для валидации (включая все колонки)
    private readonly Dictionary<string, string[]> _tableColumns = new()
    {
        { "Books", new[] { "book_id", "isbn", "title", "description", "publication_year", "publisher_id", "language_id", "series_id", "cover_image_path", "default_copy_count" } },
        { "Users", new[] { "user_id", "username", "email", "password_hash", "first_name", "last_name", "date_of_birth", "registration_date", "role_id", "is_active", "confirmation_token", "is_email_confirmed", "password_reset_token", "reset_token_expiration" } },
        { "Loans", new[] { "loan_id", "copy_id", "user_id", "loan_date", "due_date", "return_date" } },
        { "BookCopies", new[] { "copy_id", "book_id", "branch_id", "status", "acquisition_date" } },
        { "Reviews", new[] { "review_id", "book_id", "user_id", "rating", "comment", "review_date" } },
        { "Authors", new[] { "author_id", "first_name", "last_name", "date_of_birth", "country", "biography" } },
        { "Genres", new[] { "genre_id", "genre_name" } },
        { "Publishers", new[] { "publisher_id", "publisher_name", "country" } },
        { "Languages", new[] { "language_id", "language_name" } },
        { "Branches", new[] { "branch_id", "branch_name", "address", "city" } }
    };

    public ImportService(DatabaseConnection db, ILogger<ImportService>? logger = null)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> ImportFromCsvAsync(Stream fileStream)
    {
        try
        {
            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                TrimOptions = CsvHelper.Configuration.TrimOptions.Trim
            });

            var tables = new Dictionary<string, List<Dictionary<string, string>>>();
            string? currentTable = null;
            string[]? currentColumns = null;
            bool isHeaderRow = false;

            // Парсим CSV построчно
            while (await csv.ReadAsync())
            {
                var record = csv.Parser.Record;
                if (record == null || record.Length == 0) continue;

                var firstField = record[0]?.Trim() ?? "";
                
                // Проверяем, является ли строка заголовком таблицы (=== TableName ===)
                if (firstField.StartsWith("===") && firstField.EndsWith("==="))
                {
                    currentTable = firstField.Replace("===", "").Trim();
                    if (!_tableColumns.ContainsKey(currentTable))
                    {
                        return (false, $"Неизвестная таблица: {currentTable}");
                    }
                    currentColumns = _tableColumns[currentTable];
                    tables[currentTable] = new List<Dictionary<string, string>>();
                    isHeaderRow = true;
                    continue;
                }

                // Если это заголовок колонок (следующая строка после заголовка таблицы)
                if (isHeaderRow && currentTable != null && currentColumns != null)
                {
                    // Проверяем, что колонки совпадают
                    var headerColumns = record.Select(c => c?.Trim().ToLower() ?? "").ToArray();
                    var expectedColumns = currentColumns.Select(c => c.ToLower()).ToArray();
                    
                    if (headerColumns.Length != expectedColumns.Length)
                    {
                        return (false, $"Неверный формат файла. Таблица {currentTable}: количество колонок не совпадает. Ожидалось: {expectedColumns.Length}, получено: {headerColumns.Length}");
                    }
                    
                    if (!headerColumns.SequenceEqual(expectedColumns))
                    {
                        return (false, $"Неверный формат файла. Таблица {currentTable}: колонки не совпадают. Ожидалось: {string.Join(", ", expectedColumns)}, получено: {string.Join(", ", headerColumns)}");
                    }
                    isHeaderRow = false;
                    continue;
                }

                // Если это строка данных
                if (currentTable != null && currentColumns != null && !isHeaderRow && record.Length == currentColumns.Length)
                {
                    var row = new Dictionary<string, string>();
                    for (int i = 0; i < currentColumns.Length; i++)
                    {
                        row[currentColumns[i]] = record[i]?.Trim() ?? "";
                    }
                    tables[currentTable].Add(row);
                }
                else if (currentTable != null && !isHeaderRow && record.Length != currentColumns?.Length)
                {
                    return (false, $"Неверный формат файла. Таблица {currentTable}: неверное количество колонок в строке данных. Ожидалось: {currentColumns?.Length}, получено: {record.Length}");
                }
            }

            // Валидация структуры
            foreach (var tableName in _tableColumns.Keys)
            {
                if (!tables.ContainsKey(tableName))
                {
                    return (false, $"Неверный формат файла. Отсутствует таблица: {tableName}");
                }
            }

            // Импортируем данные в транзакции
            return await ImportDataAsync(tables);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при импорте CSV");
            return (false, $"Ошибка при импорте: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> ImportFromXmlAsync(Stream fileStream)
    {
        try
        {
            var tables = new Dictionary<string, List<Dictionary<string, string>>>();
            
            using var reader = XmlReader.Create(fileStream, new XmlReaderSettings { Async = true });
            
            string? currentTable = null;
            Dictionary<string, string>? currentRow = null;
            string? currentColumn = null;

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "Database")
                    {
                        // Начало документа
                        continue;
                    }
                    else if (reader.Name == "Table")
                    {
                        currentTable = reader.GetAttribute("name");
                        if (currentTable == null || !_tableColumns.ContainsKey(currentTable))
                        {
                            return (false, $"Неизвестная таблица: {currentTable}");
                        }
                        tables[currentTable] = new List<Dictionary<string, string>>();
                    }
                    else if (reader.Name == "Row")
                    {
                        currentRow = new Dictionary<string, string>();
                    }
                    else if (currentTable != null && _tableColumns[currentTable].Contains(reader.Name))
                    {
                        currentColumn = reader.Name;
                    }
                }
                else if (reader.NodeType == XmlNodeType.Text && currentColumn != null && currentRow != null)
                {
                    currentRow[currentColumn] = reader.Value;
                }
                else if (reader.NodeType == XmlNodeType.EndElement)
                {
                    if (reader.Name == "Row" && currentRow != null && currentTable != null)
                    {
                        // Проверяем, что все колонки присутствуют
                        var expectedColumns = _tableColumns[currentTable];
                        foreach (var col in expectedColumns)
                        {
                            if (!currentRow.ContainsKey(col))
                            {
                                currentRow[col] = "";
                            }
                        }
                        tables[currentTable].Add(currentRow);
                        currentRow = null;
                    }
                    else if (reader.Name == "Table")
                    {
                        currentTable = null;
                    }
                    currentColumn = null;
                }
            }

            // Валидация структуры
            foreach (var tableName in _tableColumns.Keys)
            {
                if (!tables.ContainsKey(tableName))
                {
                    return (false, $"Неверный формат файла. Отсутствует таблица: {tableName}");
                }
            }

            // Импортируем данные в транзакции
            return await ImportDataAsync(tables);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при импорте XML");
            return (false, $"Ошибка при импорте: {ex.Message}");
        }
    }

    private async Task<(bool Success, string Message)> ImportDataAsync(Dictionary<string, List<Dictionary<string, string>>> tables)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        
        // Используем явную транзакцию Npgsql вместо TransactionScope
        using var transaction = await conn.BeginTransactionAsync();
        
        try
        {
            // Очищаем таблицы в обратном порядке зависимостей
            // Сначала отключаем внешние ключи временно (если нужно)
            var tablesToClear = new[] { "Loans", "Reviews", "BookCopies", "BookGenres", "BookAuthors", "Books", "Users", "Authors", "Genres", "Publishers", "Languages", "Branches" };
            
            // Используем DELETE вместо TRUNCATE для лучшей совместимости с транзакциями
            foreach (var tableName in tablesToClear)
            {
                try
                {
                    var deleteSql = $"DELETE FROM \"{tableName}\"";
                    using var cmd = new NpgsqlCommand(deleteSql, conn, transaction);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Не удалось очистить таблицу {TableName}: {Message}", tableName, ex.Message);
                    // Если не удалось очистить, пробуем TRUNCATE
                    try
                    {
                        var truncateSql = $"TRUNCATE TABLE \"{tableName}\" CASCADE";
                        using var cmd = new NpgsqlCommand(truncateSql, conn, transaction);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex2)
                    {
                        _logger?.LogWarning(ex2, "Не удалось очистить таблицу {TableName} через TRUNCATE", tableName);
                        // Если и это не помогло, пропускаем таблицу
                    }
                }
            }

            // Вставляем данные в правильном порядке
            var insertOrder = new[] { "Branches", "Languages", "Publishers", "Genres", "Authors", "Users", "Books", "BookCopies", "Loans", "Reviews" };

            foreach (var tableName in insertOrder)
            {
                if (!tables.ContainsKey(tableName) || tables[tableName].Count == 0)
                {
                    _logger?.LogInformation("Пропуск таблицы {TableName} - нет данных", tableName);
                    continue;
                }

                _logger?.LogInformation("Вставка данных в таблицу {TableName}, строк: {Count}", tableName, tables[tableName].Count);

                var columns = _tableColumns[tableName];
                var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
                var valuePlaceholders = string.Join(", ", columns.Select((_, i) => $"@p{i}"));

                int rowIndex = 0;
                foreach (var row in tables[tableName])
                {
                    rowIndex++;
                    try
                    {
                        var insertSql = $"INSERT INTO \"{tableName}\" ({columnList}) VALUES ({valuePlaceholders})";
                        using var cmd = new NpgsqlCommand(insertSql, conn, transaction);
                    
                    for (int i = 0; i < columns.Length; i++)
                    {
                        var columnName = columns[i];
                        var value = row.ContainsKey(columnName) ? row[columnName] : "";
                        
                        if (string.IsNullOrEmpty(value))
                        {
                            cmd.Parameters.AddWithValue($"@p{i}", DBNull.Value);
                        }
                        else
                        {
                            // Пытаемся определить тип данных
                            if (columnName.EndsWith("_id") || columnName == "book_id" || columnName == "user_id" || columnName == "copy_id" || columnName == "loan_id" || columnName == "review_id" || columnName == "author_id" || columnName == "genre_id" || columnName == "publisher_id" || columnName == "language_id" || columnName == "branch_id" || columnName == "role_id")
                            {
                                if (Guid.TryParse(value, out var guidValue))
                                {
                                    cmd.Parameters.AddWithValue($"@p{i}", guidValue);
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue($"@p{i}", DBNull.Value);
                                }
                            }
                            else if (columnName.Contains("date") || columnName.Contains("_date") || columnName == "reset_token_expiration")
                            {
                                if (DateTime.TryParse(value, out var dateValue))
                                {
                                    cmd.Parameters.AddWithValue($"@p{i}", dateValue);
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue($"@p{i}", DBNull.Value);
                                }
                            }
                            else if (columnName.Contains("year") || columnName.Contains("rating") || columnName.Contains("count"))
                            {
                                if (int.TryParse(value, out var intValue))
                                {
                                    cmd.Parameters.AddWithValue($"@p{i}", intValue);
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue($"@p{i}", DBNull.Value);
                                }
                            }
                            else if (columnName == "is_active" || columnName == "is_email_confirmed")
                            {
                                if (bool.TryParse(value, out var boolValue))
                                {
                                    cmd.Parameters.AddWithValue($"@p{i}", boolValue);
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue($"@p{i}", false);
                                }
                            }
                            else
                            {
                                // Для всех остальных колонок (включая password_hash, confirmation_token, password_reset_token) - строка
                                cmd.Parameters.AddWithValue($"@p{i}", value);
                            }
                        }
                    }

                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Ошибка при вставке строки {RowIndex} в таблицу {TableName}", rowIndex, tableName);
                        throw new Exception($"Ошибка при вставке данных в таблицу {tableName}, строка {rowIndex}: {ex.Message}", ex);
                    }
                }
                
                _logger?.LogInformation("Успешно вставлено {Count} строк в таблицу {TableName}", tables[tableName].Count, tableName);
            }

            // Логируем в AuditLogs
            await LogImportAsync(conn, transaction);

            await transaction.CommitAsync();
            return (true, "Импорт завершён. БД обновлена.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger?.LogError(ex, "Ошибка при импорте данных");
            return (false, $"Ошибка при импорте данных: {ex.Message}");
        }
    }

    private async Task LogImportAsync(NpgsqlConnection conn, NpgsqlTransaction? transaction = null)
    {
        try
        {
            var logSql = @"INSERT INTO AuditLogs (action, log_date, details) 
                          VALUES ('Database Import', CURRENT_TIMESTAMP, 'Импорт данных из файла выполнен успешно')";
            using var cmd = new NpgsqlCommand(logSql, conn, transaction);
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Игнорируем ошибки логирования
        }
    }
}

