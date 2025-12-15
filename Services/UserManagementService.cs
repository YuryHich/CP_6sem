using Npgsql;
using LibraryManagement.DataAccess;
using LibraryManagement.DTOs;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Services;

public class UserManagementService : IUserManagementService
{
    private readonly DatabaseConnection _db;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(DatabaseConnection db, ILogger<UserManagementService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<UserManagementDTO>> GetAllUsersAsync()
    {
        var users = new List<UserManagementDTO>();
        
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT u.user_id, u.username, u.email, u.registration_date, r.role_name, u.is_active
            FROM Users u
            INNER JOIN Roles r ON u.role_id = r.role_id
            WHERE u.is_active = TRUE
            ORDER BY u.registration_date DESC";

        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            users.Add(new UserManagementDTO
            {
                UserId = reader.GetGuid(0),
                Username = reader.GetString(1),
                Email = reader.GetString(2),
                RegistrationDate = reader.GetDateTime(3),
                RoleName = reader.GetString(4),
                IsActive = reader.GetBoolean(5)
            });
        }

        return users;
    }

    public async Task<bool> CanDeleteUserAsync(Guid userId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        // Проверяем, не последний ли это админ
        var sql = @"
            SELECT COUNT(*) 
            FROM Users u
            INNER JOIN Roles r ON u.role_id = r.role_id
            WHERE r.role_name = 'admin' AND u.is_active = TRUE";

        using var cmd = new NpgsqlCommand(sql, conn);
        var adminCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        if (adminCount <= 1)
        {
            // Проверяем, является ли удаляемый пользователь админом
            var checkAdminSql = @"
                SELECT COUNT(*) 
                FROM Users u
                INNER JOIN Roles r ON u.role_id = r.role_id
                WHERE u.user_id = @userId AND r.role_name = 'admin'";

            using var checkCmd = new NpgsqlCommand(checkAdminSql, conn);
            checkCmd.Parameters.AddWithValue("@userId", userId);
            var isAdmin = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

            if (isAdmin)
            {
                _logger.LogWarning("Попытка удалить последнего админа: {UserId}", userId);
                return false;
            }
        }

        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            // Проверяем возможность удаления
            if (!await CanDeleteUserAsync(userId))
            {
                _logger.LogWarning("Невозможно удалить пользователя {UserId} - последний админ", userId);
                return false;
            }

            // Удаляем пользователя (каскадное удаление настроено в БД)
            var deleteSql = "DELETE FROM Users WHERE user_id = @userId";
            using var cmd = new NpgsqlCommand(deleteSql, conn, transaction);
            cmd.Parameters.AddWithValue("@userId", userId);
            var rowsAffected = await cmd.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                // Логируем в AuditLogs
                var logSql = @"
                    INSERT INTO AuditLogs (action, log_date, details) 
                    VALUES ('User Deleted', CURRENT_TIMESTAMP, @details)";
                using var logCmd = new NpgsqlCommand(logSql, conn, transaction);
                logCmd.Parameters.AddWithValue("@details", $"Удалён пользователь с ID: {userId}");
                await logCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                _logger.LogInformation("Пользователь {UserId} успешно удалён", userId);
                return true;
            }

            await transaction.RollbackAsync();
            return false;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Ошибка при удалении пользователя {UserId}", userId);
            throw;
        }
    }
}

