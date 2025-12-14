using Npgsql;
using LibraryManagement.Models;
using LibraryManagement.DTOs;

namespace LibraryManagement.DataAccess;

public class UserRepository
{
    private readonly DatabaseConnection _db;

    public UserRepository(DatabaseConnection db)
    {
        _db = db;
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT u.user_id, u.username, u.email, u.password_hash, u.first_name, u.last_name,
                   u.date_of_birth, u.registration_date, u.role_id, u.is_active, r.role_name,
                   u.confirmation_token, u.is_email_confirmed, u.password_reset_token, u.reset_token_expiration
            FROM Users u
            INNER JOIN Roles r ON u.role_id = r.role_id
            WHERE u.username = @username";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@username", username);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                UserId = reader.GetGuid(0),
                Username = reader.GetString(1),
                Email = reader.GetString(2),
                PasswordHash = reader.GetString(3),
                FirstName = reader.IsDBNull(4) ? null : reader.GetString(4),
                LastName = reader.IsDBNull(5) ? null : reader.GetString(5),
                DateOfBirth = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                RegistrationDate = reader.GetDateTime(7),
                RoleId = reader.GetGuid(8),
                IsActive = reader.GetBoolean(9),
                ConfirmationToken = reader.IsDBNull(11) ? null : reader.GetString(11),
                IsEmailConfirmed = reader.IsDBNull(12) ? false : reader.GetBoolean(12),
                PasswordResetToken = reader.IsDBNull(13) ? null : reader.GetString(13),
                ResetTokenExpiration = reader.IsDBNull(14) ? null : reader.GetDateTime(14)
            };
        }

        return null;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"SELECT user_id, username, email, password_hash, first_name, last_name, date_of_birth, 
                           registration_date, role_id, is_active, confirmation_token, is_email_confirmed, 
                           password_reset_token, reset_token_expiration 
                    FROM Users WHERE email = @email";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@email", email);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                UserId = reader.GetGuid(0),
                Username = reader.GetString(1),
                Email = reader.GetString(2),
                PasswordHash = reader.GetString(3),
                FirstName = reader.IsDBNull(4) ? null : reader.GetString(4),
                LastName = reader.IsDBNull(5) ? null : reader.GetString(5),
                DateOfBirth = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                RegistrationDate = reader.GetDateTime(7),
                RoleId = reader.GetGuid(8),
                IsActive = reader.GetBoolean(9),
                ConfirmationToken = reader.IsDBNull(10) ? null : reader.GetString(10),
                IsEmailConfirmed = reader.IsDBNull(11) ? false : reader.GetBoolean(11),
                PasswordResetToken = reader.IsDBNull(12) ? null : reader.GetString(12),
                ResetTokenExpiration = reader.IsDBNull(13) ? null : reader.GetDateTime(13)
            };
        }

        return null;
    }

    public async Task<Guid> CreateUserAsync(RegisterDTO registerDto, string passwordHash, string confirmationToken)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        // Получаем role_id для 'user'
        var roleSql = "SELECT role_id FROM Roles WHERE role_name = 'user'";
        using var roleCmd = new NpgsqlCommand(roleSql, conn);
        var roleIdObj = await roleCmd.ExecuteScalarAsync();
        if (roleIdObj == null)
        {
            throw new Exception("Role 'user' not found");
        }
        var roleId = (Guid)roleIdObj;

        var userId = Guid.NewGuid();
        var sql = @"
            INSERT INTO Users (user_id, username, email, password_hash, first_name, last_name, date_of_birth, role_id, confirmation_token, is_email_confirmed)
            VALUES (@userId, @username, @email, @passwordHash, @firstName, @lastName, @dateOfBirth, @roleId, @confirmationToken, FALSE)";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@username", registerDto.Username);
        cmd.Parameters.AddWithValue("@email", registerDto.Email);
        cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
        cmd.Parameters.AddWithValue("@firstName", (object?)registerDto.FirstName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@lastName", (object?)registerDto.LastName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@dateOfBirth", (object?)registerDto.DateOfBirth ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@roleId", roleId);
        cmd.Parameters.AddWithValue("@confirmationToken", confirmationToken);

        await cmd.ExecuteNonQueryAsync();
        return userId;
    }

    public async Task<bool> ConfirmEmailAsync(string email, string token)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            UPDATE Users 
            SET is_email_confirmed = TRUE, confirmation_token = NULL
            WHERE email = @email AND confirmation_token = @token AND is_email_confirmed = FALSE";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@token", token);

        var rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> SetPasswordResetTokenAsync(string email, string token, DateTime expiration)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            UPDATE Users 
            SET password_reset_token = @token, reset_token_expiration = @expiration
            WHERE email = @email";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@token", token);
        cmd.Parameters.AddWithValue("@expiration", expiration);

        var rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<User?> GetUserByResetTokenAsync(string email, string token)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT user_id, username, email, password_hash, first_name, last_name, date_of_birth, 
                   registration_date, role_id, is_active, confirmation_token, is_email_confirmed, 
                   password_reset_token, reset_token_expiration 
            FROM Users 
            WHERE email = @email AND password_reset_token = @token 
                  AND reset_token_expiration > CURRENT_TIMESTAMP";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@token", token);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                UserId = reader.GetGuid(0),
                Username = reader.GetString(1),
                Email = reader.GetString(2),
                PasswordHash = reader.GetString(3),
                FirstName = reader.IsDBNull(4) ? null : reader.GetString(4),
                LastName = reader.IsDBNull(5) ? null : reader.GetString(5),
                DateOfBirth = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                RegistrationDate = reader.GetDateTime(7),
                RoleId = reader.GetGuid(8),
                IsActive = reader.GetBoolean(9),
                ConfirmationToken = reader.IsDBNull(10) ? null : reader.GetString(10),
                IsEmailConfirmed = reader.IsDBNull(11) ? false : reader.GetBoolean(11),
                PasswordResetToken = reader.IsDBNull(12) ? null : reader.GetString(12),
                ResetTokenExpiration = reader.IsDBNull(13) ? null : reader.GetDateTime(13)
            };
        }

        return null;
    }

    public async Task<bool> ResetPasswordAsync(Guid userId, string newPasswordHash)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            UPDATE Users 
            SET password_hash = @passwordHash, password_reset_token = NULL, reset_token_expiration = NULL
            WHERE user_id = @userId";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@passwordHash", newPasswordHash);

        var rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<string> GetUserRoleAsync(Guid userId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT r.role_name
            FROM Users u
            INNER JOIN Roles r ON u.role_id = r.role_id
            WHERE u.user_id = @userId";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@userId", userId);

        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString() ?? "user";
    }

    public async Task<List<LoanDTO>> GetUserLoansAsync(Guid userId)
    {
        var loans = new List<LoanDTO>();
        
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT l.loan_id, b.book_id, b.title, b.isbn, l.loan_date, l.due_date, l.return_date
            FROM Loans l
            INNER JOIN BookCopies bc ON l.copy_id = bc.copy_id
            INNER JOIN Books b ON bc.book_id = b.book_id
            WHERE l.user_id = @userId
            ORDER BY l.loan_date DESC";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@userId", userId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var dueDate = reader.GetDateTime(5);
            var returnDate = reader.IsDBNull(6) ? null : (DateTime?)reader.GetDateTime(6);
            var isOverdue = returnDate == null && DateTime.UtcNow > dueDate;

            loans.Add(new LoanDTO
            {
                LoanId = reader.GetGuid(0),
                BookId = reader.GetGuid(1),
                BookTitle = reader.GetString(2),
                Isbn = reader.GetString(3),
                LoanDate = reader.GetDateTime(4),
                DueDate = dueDate,
                ReturnDate = returnDate,
                IsOverdue = isOverdue
            });
        }

        return loans;
    }

    public async Task<Guid?> GetCopyIdByLoanIdAsync(Guid loanId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = "SELECT copy_id FROM Loans WHERE loan_id = @loanId";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@loanId", loanId);

        var result = await cmd.ExecuteScalarAsync();
        return result != null ? (Guid?)result : null;
    }
}

