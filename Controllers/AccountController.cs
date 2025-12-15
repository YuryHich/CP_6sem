using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibraryManagement.Services;
using LibraryManagement.DTOs;
using LibraryManagement.DataAccess;
using System.Security.Claims;
using Npgsql;
using BCrypt.Net;

namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly DatabaseConnection _db;
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        DatabaseConnection db,
        IUserManagementService userManagementService,
        ILogger<AccountController> logger)
    {
        _db = db;
        _userManagementService = userManagementService;
        _logger = logger;
    }

    [HttpGet("check-last-admin")]
    public async Task<IActionResult> CheckLastAdmin()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "Пользователь не авторизован" });
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Проверяем, является ли пользователь админом
            var checkAdminSql = @"
                SELECT COUNT(*) 
                FROM Users u
                INNER JOIN Roles r ON u.role_id = r.role_id
                WHERE u.user_id = @userId AND r.role_name = 'admin'";

            using var checkCmd = new NpgsqlCommand(checkAdminSql, conn);
            checkCmd.Parameters.AddWithValue("@userId", userId);
            var isAdmin = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

            if (!isAdmin)
            {
                return Ok(new { isLastAdmin = false });
            }

            // Считаем количество админов
            var countSql = @"
                SELECT COUNT(*) 
                FROM Users u
                INNER JOIN Roles r ON u.role_id = r.role_id
                WHERE r.role_name = 'admin' AND u.is_active = TRUE";

            using var countCmd = new NpgsqlCommand(countSql, conn);
            var adminCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            return Ok(new { isLastAdmin = adminCount <= 1 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке последнего админа");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountDTO dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "Пользователь не авторизован" });
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Получаем данные пользователя для проверки пароля
            var getUserSql = @"
                SELECT password_hash, r.role_name
                FROM Users u
                INNER JOIN Roles r ON u.role_id = r.role_id
                WHERE u.user_id = @userId";

            using var getUserCmd = new NpgsqlCommand(getUserSql, conn);
            getUserCmd.Parameters.AddWithValue("@userId", userId);
            
            string? passwordHash = null;
            string? roleName = null;

            using var reader = await getUserCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                passwordHash = reader.GetString(0);
                roleName = reader.GetString(1);
            }
            else
            {
                return NotFound(new { error = "Пользователь не найден" });
            }

            await reader.CloseAsync();

            // Проверяем пароль
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, passwordHash))
            {
                return BadRequest(new { error = "Неверный пароль" });
            }

            // Проверяем, не последний ли это админ
            if (roleName == "admin")
            {
                var countSql = @"
                    SELECT COUNT(*) 
                    FROM Users u
                    INNER JOIN Roles r ON u.role_id = r.role_id
                    WHERE r.role_name = 'admin' AND u.is_active = TRUE";

                using var countCmd = new NpgsqlCommand(countSql, conn);
                var adminCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

                if (adminCount <= 1)
                {
                    return BadRequest(new { error = "Невозможно удалить последнего администратора" });
                }
            }

            // Удаляем пользователя
            var canDelete = await _userManagementService.CanDeleteUserAsync(userId);
            if (!canDelete)
            {
                return BadRequest(new { error = "Невозможно удалить аккаунт" });
            }

            var deleted = await _userManagementService.DeleteUserAsync(userId);
            if (deleted)
            {
                _logger.LogInformation("Пользователь {UserId} удалил свой аккаунт", userId);
                return Ok(new { message = "Аккаунт успешно удалён" });
            }

            return BadRequest(new { error = "Не удалось удалить аккаунт" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении аккаунта");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

