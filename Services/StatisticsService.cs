using Npgsql;
using LibraryManagement.DataAccess;
using LibraryManagement.DTOs;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Services;

public class StatisticsService : IStatisticsService
{
    private readonly DatabaseConnection _db;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(DatabaseConnection db, ILogger<StatisticsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<GlobalStatisticsDTO> GetGlobalStatisticsAsync()
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT 
                (SELECT COUNT(*) FROM Books) as total_books,
                (SELECT COUNT(*) FROM Loans) as total_loans,
                (SELECT COUNT(*) FROM Loans WHERE return_date IS NULL) as active_loans,
                (SELECT COUNT(*) FROM Loans WHERE return_date IS NULL AND due_date < CURRENT_TIMESTAMP) as overdue_loans,
                (SELECT COUNT(*) FROM Users) as total_users,
                (SELECT COUNT(*) FROM Authors) as total_authors";

        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new GlobalStatisticsDTO
            {
                TotalBooks = reader.GetInt32(0),
                TotalLoans = reader.GetInt32(1),
                ActiveLoans = reader.GetInt32(2),
                OverdueLoans = reader.GetInt32(3),
                TotalUsers = reader.GetInt32(4),
                TotalAuthors = reader.GetInt32(5)
            };
        }

        return new GlobalStatisticsDTO();
    }

    public async Task<UserStatisticsDTO> GetUserStatisticsAsync(Guid userId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        // Общая статистика пользователя
        var sql = @"
            SELECT 
                COUNT(*) as total_loans,
                COUNT(*) FILTER (WHERE return_date IS NULL) as active_loans,
                COUNT(*) FILTER (WHERE return_date IS NULL AND due_date < CURRENT_TIMESTAMP) as overdue_loans
            FROM Loans
            WHERE user_id = @userId";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@userId", userId);
        using var reader = await cmd.ExecuteReaderAsync();

        var stats = new UserStatisticsDTO();

        if (await reader.ReadAsync())
        {
            stats.TotalLoans = reader.GetInt32(0);
            stats.ActiveLoans = reader.GetInt32(1);
            stats.OverdueLoans = reader.GetInt32(2);
        }

        await reader.CloseAsync();

        // Любимые жанры пользователя
        var genreSql = @"
            SELECT g.genre_name, COUNT(*) as count
            FROM Loans l
            INNER JOIN BookCopies bc ON l.copy_id = bc.copy_id
            INNER JOIN Books b ON bc.book_id = b.book_id
            INNER JOIN BookGenres bg ON b.book_id = bg.book_id
            INNER JOIN Genres g ON bg.genre_id = g.genre_id
            WHERE l.user_id = @userId
            GROUP BY g.genre_name
            ORDER BY count DESC
            LIMIT 5";

        using var genreCmd = new NpgsqlCommand(genreSql, conn);
        genreCmd.Parameters.AddWithValue("@userId", userId);
        using var genreReader = await genreCmd.ExecuteReaderAsync();

        while (await genreReader.ReadAsync())
        {
            stats.FavoriteGenres.Add(new GenreCountDTO
            {
                GenreName = genreReader.GetString(0),
                Count = genreReader.GetInt32(1)
            });
        }

        return stats;
    }

    public async Task<List<AuthorStatisticsDTO>> GetAuthorStatisticsAsync()
    {
        var authors = new List<AuthorStatisticsDTO>();
        
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT 
                a.first_name || ' ' || a.last_name as author_name,
                COUNT(DISTINCT ba.book_id) as book_count,
                COUNT(l.loan_id) as loan_count
            FROM Authors a
            LEFT JOIN BookAuthors ba ON a.author_id = ba.author_id
            LEFT JOIN Books b ON ba.book_id = b.book_id
            LEFT JOIN BookCopies bc ON b.book_id = bc.book_id
            LEFT JOIN Loans l ON bc.copy_id = l.copy_id
            GROUP BY a.author_id, a.first_name, a.last_name
            ORDER BY loan_count DESC
            LIMIT 15";

        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            authors.Add(new AuthorStatisticsDTO
            {
                AuthorName = reader.GetString(0),
                BookCount = reader.GetInt32(1),
                LoanCount = reader.GetInt32(2)
            });
        }

        return authors;
    }

    public async Task<List<GenreStatisticsDTO>> GetGenreStatisticsAsync()
    {
        var genres = new List<GenreStatisticsDTO>();
        
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT 
                g.genre_name,
                COUNT(DISTINCT bg.book_id) as book_count,
                COUNT(l.loan_id) as loan_count
            FROM Genres g
            LEFT JOIN BookGenres bg ON g.genre_id = bg.genre_id
            LEFT JOIN Books b ON bg.book_id = b.book_id
            LEFT JOIN BookCopies bc ON b.book_id = bc.book_id
            LEFT JOIN Loans l ON bc.copy_id = l.copy_id
            GROUP BY g.genre_id, g.genre_name
            ORDER BY loan_count DESC";

        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            genres.Add(new GenreStatisticsDTO
            {
                GenreName = reader.GetString(0),
                BookCount = reader.GetInt32(1),
                LoanCount = reader.GetInt32(2)
            });
        }

        return genres;
    }

    public async Task<List<LanguageStatisticsDTO>> GetLanguageStatisticsAsync()
    {
        var languages = new List<LanguageStatisticsDTO>();
        
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT 
                l.language_name,
                COUNT(DISTINCT b.book_id) as book_count,
                COUNT(lo.loan_id) as loan_count
            FROM Languages l
            LEFT JOIN Books b ON l.language_id = b.language_id
            LEFT JOIN BookCopies bc ON b.book_id = bc.book_id
            LEFT JOIN Loans lo ON bc.copy_id = lo.copy_id
            GROUP BY l.language_id, l.language_name
            ORDER BY loan_count DESC";

        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            languages.Add(new LanguageStatisticsDTO
            {
                LanguageName = reader.GetString(0),
                BookCount = reader.GetInt32(1),
                LoanCount = reader.GetInt32(2)
            });
        }

        return languages;
    }
}

