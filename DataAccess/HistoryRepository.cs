using LibraryManagement.DTOs;
using Npgsql;

namespace LibraryManagement.DataAccess;

public class HistoryRepository
{
    private readonly DatabaseConnection _db;

    public HistoryRepository(DatabaseConnection db)
    {
        _db = db;
    }

    public Task<List<HistoryRecordDTO>> GetBooksHistoryAsync(
        DateTime? fromDate,
        DateTime? toDate,
        IReadOnlyCollection<string> operationTypes,
        string? usernameFilter)
    {
        return GetHistoryInternalAsync("BooksHistory", fromDate, toDate, operationTypes, usernameFilter);
    }

    public Task<List<HistoryRecordDTO>> GetLoansHistoryAsync(
        DateTime? fromDate,
        DateTime? toDate,
        IReadOnlyCollection<string> operationTypes,
        string? usernameFilter)
    {
        return GetHistoryInternalAsync("LoansHistory", fromDate, toDate, operationTypes, usernameFilter);
    }

    private async Task<List<HistoryRecordDTO>> GetHistoryInternalAsync(
        string tableName,
        DateTime? fromDate,
        DateTime? toDate,
        IReadOnlyCollection<string> operationTypes,
        string? usernameFilter)
    {
        var records = new List<HistoryRecordDTO>();

        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = $@"
            SELECT history_id, operation_date, operation_type, old_value, new_value, username
            FROM {tableName}
            WHERE 1 = 1";

        using var cmd = new NpgsqlCommand();
        cmd.Connection = conn;

        if (fromDate.HasValue)
        {
            sql += " AND operation_date >= @fromDate";
            cmd.Parameters.AddWithValue("@fromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            sql += " AND operation_date <= @toDate";
            cmd.Parameters.AddWithValue("@toDate", toDate.Value);
        }

        if (operationTypes != null && operationTypes.Count > 0)
        {
            // operation_type IN ('Insert','Update',...)
            var paramNames = new List<string>();
            var index = 0;
            foreach (var op in operationTypes)
            {
                var paramName = $"@op{index++}";
                paramNames.Add(paramName);
                cmd.Parameters.AddWithValue(paramName, op);
            }

            sql += $" AND operation_type IN ({string.Join(",", paramNames)})";
        }

        if (!string.IsNullOrWhiteSpace(usernameFilter))
        {
            sql += " AND username ILIKE @username";
            cmd.Parameters.AddWithValue("@username", $"%{usernameFilter.Trim()}%");
        }

        sql += " ORDER BY operation_date DESC LIMIT 500";
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add(new HistoryRecordDTO
            {
                HistoryId = reader.GetGuid(0),
                OperationDate = reader.GetDateTime(1),
                OperationType = reader.GetString(2),
                OldValue = reader.IsDBNull(3) ? null : reader.GetString(3),
                NewValue = reader.IsDBNull(4) ? null : reader.GetString(4),
                Username = reader.GetString(5)
            });
        }

        return records;
    }

    public async Task<List<string>> GetDistinctUsernamesAsync()
    {
        var usernames = new List<string>();

        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT DISTINCT username 
            FROM (
                SELECT username FROM BooksHistory
                UNION
                SELECT username FROM LoansHistory
            ) t
            WHERE username IS NOT NULL
            ORDER BY username
            LIMIT 100";

        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            usernames.Add(reader.GetString(0));
        }

        return usernames;
    }
}


