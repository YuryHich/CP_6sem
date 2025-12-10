using LibraryManagement.DTOs;
using Npgsql;

namespace LibraryManagement.DataAccess;

public class ReviewRepository
{
    private readonly DatabaseConnection _db;

    public ReviewRepository(DatabaseConnection db)
    {
        _db = db;
    }

    public async Task<List<ReviewDTO>> GetReviewsByBookAsync(Guid bookId)
    {
        var reviews = new List<ReviewDTO>();

        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT r.review_id, r.book_id, r.user_id, u.username, r.rating, r.comment, r.review_date
            FROM Reviews r
            INNER JOIN Users u ON u.user_id = r.user_id
            WHERE r.book_id = @bookId
            ORDER BY r.review_date DESC";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@bookId", bookId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            reviews.Add(new ReviewDTO
            {
                ReviewId = reader.GetGuid(0),
                BookId = reader.GetGuid(1),
                UserId = reader.GetGuid(2),
                Username = reader.GetString(3),
                Rating = reader.GetInt32(4),
                Comment = reader.IsDBNull(5) ? null : reader.GetString(5),
                ReviewDate = reader.GetDateTime(6)
            });
        }

        return reviews;
    }

    public async Task<bool> UserHasReviewAsync(Guid bookId, Guid userId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        const string sql = "SELECT COUNT(*) FROM Reviews WHERE book_id = @bookId AND user_id = @userId";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@bookId", bookId);
        cmd.Parameters.AddWithValue("@userId", userId);

        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
        return count > 0;
    }

    public async Task<ReviewDTO> CreateReviewAsync(Guid bookId, Guid userId, int rating, string? comment)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var reviewId = Guid.NewGuid();

        const string sql = @"
            INSERT INTO Reviews (review_id, book_id, user_id, rating, comment)
            VALUES (@reviewId, @bookId, @userId, @rating, @comment)
            RETURNING review_date";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@reviewId", reviewId);
        cmd.Parameters.AddWithValue("@bookId", bookId);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@rating", rating);
        cmd.Parameters.AddWithValue("@comment", (object?)comment ?? DBNull.Value);

        var reviewDateObj = await cmd.ExecuteScalarAsync();
        var reviewDate = reviewDateObj is DateTime dt ? dt : DateTime.UtcNow;

        // Получаем имя пользователя для DTO
        const string userSql = "SELECT username FROM Users WHERE user_id = @userId";
        using var userCmd = new NpgsqlCommand(userSql, conn);
        userCmd.Parameters.AddWithValue("@userId", userId);
        var usernameObj = await userCmd.ExecuteScalarAsync();

        return new ReviewDTO
        {
            ReviewId = reviewId,
            BookId = bookId,
            UserId = userId,
            Username = usernameObj as string ?? string.Empty,
            Rating = rating,
            Comment = comment,
            ReviewDate = reviewDate
        };
    }

    public async Task DeleteReviewAsync(Guid reviewId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        const string sql = "DELETE FROM Reviews WHERE review_id = @reviewId";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@reviewId", reviewId);
        await cmd.ExecuteNonQueryAsync();
    }
}




