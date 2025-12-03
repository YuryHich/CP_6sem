using Npgsql;
using LibraryManagement.Models;
using LibraryManagement.DTOs;

namespace LibraryManagement.DataAccess;

public class AuthorRepository
{
    private readonly DatabaseConnection _db;

    public AuthorRepository(DatabaseConnection db)
    {
        _db = db;
    }

    public async Task<List<AuthorDTO>> GetAuthorsAsync()
    {
        var authors = new List<AuthorDTO>();
        
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = "SELECT author_id, first_name, last_name, date_of_birth, country, biography FROM Authors ORDER BY last_name, first_name";
        using var cmd = new NpgsqlCommand(sql, conn);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            authors.Add(new AuthorDTO
            {
                AuthorId = reader.GetGuid(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                DateOfBirth = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                Country = reader.IsDBNull(4) ? null : reader.GetString(4),
                Biography = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return authors;
    }

    public async Task<AuthorDTO?> GetAuthorByIdAsync(Guid authorId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = "SELECT author_id, first_name, last_name, date_of_birth, country, biography FROM Authors WHERE author_id = @authorId";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@authorId", authorId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new AuthorDTO
            {
                AuthorId = reader.GetGuid(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                DateOfBirth = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                Country = reader.IsDBNull(4) ? null : reader.GetString(4),
                Biography = reader.IsDBNull(5) ? null : reader.GetString(5)
            };
        }

        return null;
    }

    public async Task<Guid> CreateAuthorAsync(AuthorDTO author)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var authorId = Guid.NewGuid();
        var sql = @"
            INSERT INTO Authors (author_id, first_name, last_name, date_of_birth, country, biography)
            VALUES (@authorId, @firstName, @lastName, @dateOfBirth, @country, @biography)";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@authorId", authorId);
        cmd.Parameters.AddWithValue("@firstName", author.FirstName);
        cmd.Parameters.AddWithValue("@lastName", author.LastName);
        cmd.Parameters.AddWithValue("@dateOfBirth", (object?)author.DateOfBirth ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@country", (object?)author.Country ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@biography", (object?)author.Biography ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
        return authorId;
    }

    public async Task UpdateAuthorAsync(Guid authorId, AuthorDTO author)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            UPDATE Authors 
            SET first_name = @firstName, last_name = @lastName, date_of_birth = @dateOfBirth,
                country = @country, biography = @biography
            WHERE author_id = @authorId";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@authorId", authorId);
        cmd.Parameters.AddWithValue("@firstName", author.FirstName);
        cmd.Parameters.AddWithValue("@lastName", author.LastName);
        cmd.Parameters.AddWithValue("@dateOfBirth", (object?)author.DateOfBirth ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@country", (object?)author.Country ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@biography", (object?)author.Biography ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> DeleteAuthorAsync(Guid authorId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        // Проверяем, есть ли книги у автора
        var checkSql = "SELECT COUNT(*) FROM BookAuthors WHERE author_id = @authorId";
        using var checkCmd = new NpgsqlCommand(checkSql, conn);
        checkCmd.Parameters.AddWithValue("@authorId", authorId);
        var count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());

        if (count > 0)
        {
            return false; // Нельзя удалить автора с книгами
        }

        var sql = "DELETE FROM Authors WHERE author_id = @authorId";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@authorId", authorId);
        await cmd.ExecuteNonQueryAsync();

        return true;
    }

    public async Task<List<BookDTO>> GetAuthorBooksAsync(Guid authorId)
    {
        var books = new List<BookDTO>();
        
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT DISTINCT 
                b.book_id, b.isbn, b.title, b.description, b.publication_year,
                b.publisher_id, b.language_id, b.series_id, b.cover_image_path,
                COUNT(DISTINCT CASE WHEN bc.status = 'available' THEN bc.copy_id END) as available_copies
            FROM Books b
            INNER JOIN BookAuthors ba ON b.book_id = ba.book_id
            LEFT JOIN BookCopies bc ON b.book_id = bc.book_id
            WHERE ba.author_id = @authorId
            GROUP BY b.book_id, b.isbn, b.title, b.description, b.publication_year,
                     b.publisher_id, b.language_id, b.series_id, b.cover_image_path
            ORDER BY b.title";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@authorId", authorId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var bookId = reader.GetGuid(0);
            var book = new BookDTO
            {
                BookId = bookId,
                Isbn = reader.GetString(1),
                Title = reader.GetString(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                PublicationYear = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                PublisherId = reader.IsDBNull(5) ? null : reader.GetGuid(5),
                LanguageId = reader.IsDBNull(6) ? null : reader.GetGuid(6),
                SeriesId = reader.IsDBNull(7) ? null : reader.GetGuid(7),
                CoverImagePath = reader.IsDBNull(8) ? null : reader.GetString(8),
                AvailableCopies = reader.GetInt32(9)
            };

            // Получаем авторов и жанры
            var bookRepo = new BookRepository(_db);
            var (authorIds, _) = await bookRepo.GetBookAuthorsAsync(bookId);
            var (genreIds, _) = await bookRepo.GetBookGenresAsync(bookId);
            book.AuthorIds = authorIds;
            book.GenreIds = genreIds;

            books.Add(book);
        }

        return books;
    }
}

