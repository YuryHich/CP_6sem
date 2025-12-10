using Npgsql;
using LibraryManagement.Models;
using LibraryManagement.DTOs;

namespace LibraryManagement.DataAccess;

public class BookRepository
{
    private readonly DatabaseConnection _db;

    public BookRepository(DatabaseConnection db)
    {
        _db = db;
    }

    public async Task<PagedResult<BookDTO>> GetBooksAsync(int page = 1, int size = 10, string? search = null, Guid? genreId = null, Guid? authorId = null)
    {
        var result = new PagedResult<BookDTO>();
        
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var whereClause = "WHERE 1=1";
        var filterParams = new List<(string Name, object? Value)>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClause += " AND (b.title ILIKE @search OR b.isbn ILIKE @search)";
            filterParams.Add(("@search", $"%{search}%"));
        }

        if (genreId.HasValue)
        {
            whereClause += " AND bg.genre_id = @genreId";
            filterParams.Add(("@genreId", genreId.Value));
        }

        if (authorId.HasValue)
        {
            whereClause += " AND ba.author_id = @authorId";
            filterParams.Add(("@authorId", authorId.Value));
        }

        void ApplyFilterParams(NpgsqlCommand command)
        {
            foreach (var (name, value) in filterParams)
            {
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }
        }

        var countSql = $@"
            SELECT COUNT(DISTINCT b.book_id)
            FROM Books b
            LEFT JOIN BookGenres bg ON b.book_id = bg.book_id
            LEFT JOIN BookAuthors ba ON b.book_id = ba.book_id
            {whereClause}";

        using (var countCmd = new NpgsqlCommand(countSql, conn))
        {
            ApplyFilterParams(countCmd);
            var total = await countCmd.ExecuteScalarAsync();
            result.TotalCount = total is null ? 0 : Convert.ToInt32(total);
        }

        var dataSql = $@"
            SELECT DISTINCT 
                b.book_id, b.isbn, b.title, b.description, b.publication_year,
                b.publisher_id, b.language_id, b.series_id, b.cover_image_path,
                COUNT(DISTINCT CASE WHEN bc.status = 'available' THEN bc.copy_id END) as available_copies,
                COALESCE(b.default_copy_count, 0) as default_copy_count
            FROM Books b
            LEFT JOIN BookCopies bc ON b.book_id = bc.book_id
            LEFT JOIN BookGenres bg ON b.book_id = bg.book_id
            LEFT JOIN BookAuthors ba ON b.book_id = ba.book_id
            {whereClause}
            GROUP BY b.book_id, b.isbn, b.title, b.description, b.publication_year,
                     b.publisher_id, b.language_id, b.series_id, b.cover_image_path, b.default_copy_count
            ORDER BY b.title
            LIMIT @size OFFSET @offset";

        using var cmd = new NpgsqlCommand(dataSql, conn);
        ApplyFilterParams(cmd);
        cmd.Parameters.AddWithValue("@size", size);
        cmd.Parameters.AddWithValue("@offset", (page - 1) * size);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var bookId = reader.GetGuid(0);
            var (authorIds, authorNames) = await GetBookAuthorsAsync(bookId);
            var (genreIds, genreNames) = await GetBookGenresAsync(bookId);

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
                AvailableCopies = reader.GetInt32(9),
                CopiesCount = reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
                AuthorIds = authorIds,
                AuthorNames = authorNames,
                GenreIds = genreIds,
                GenreNames = genreNames
            };

            result.Items.Add(book);
        }

        return result;
    }

    public async Task<BookDTO?> GetBookByIdAsync(Guid bookId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT b.book_id, b.isbn, b.title, b.description, b.publication_year,
                   b.publisher_id, b.language_id, b.series_id, b.cover_image_path,
                   COUNT(DISTINCT CASE WHEN bc.status = 'available' THEN bc.copy_id END) as available_copies,
                   COALESCE(b.default_copy_count, 0) as default_copy_count
            FROM Books b
            LEFT JOIN BookCopies bc ON b.book_id = bc.book_id
            WHERE b.book_id = @bookId
            GROUP BY b.book_id, b.isbn, b.title, b.description, b.publication_year,
                     b.publisher_id, b.language_id, b.series_id, b.cover_image_path, b.default_copy_count";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@bookId", bookId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var book = new BookDTO
            {
                BookId = reader.GetGuid(0),
                Isbn = reader.GetString(1),
                Title = reader.GetString(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                PublicationYear = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                PublisherId = reader.IsDBNull(5) ? null : reader.GetGuid(5),
                LanguageId = reader.IsDBNull(6) ? null : reader.GetGuid(6),
                SeriesId = reader.IsDBNull(7) ? null : reader.GetGuid(7),
                CoverImagePath = reader.IsDBNull(8) ? null : reader.GetString(8),
                AvailableCopies = reader.GetInt32(9),
                CopiesCount = reader.IsDBNull(10) ? 0 : reader.GetInt32(10)
            };

            var (authorIds, authorNames) = await GetBookAuthorsAsync(bookId);
            var (genreIds, genreNames) = await GetBookGenresAsync(bookId);
            book.AuthorIds = authorIds;
            book.AuthorNames = authorNames;
            book.GenreIds = genreIds;
            book.GenreNames = genreNames;
            book.BranchAvailability = await GetBookBranchAvailabilityAsync(bookId);

            return book;
        }

        return null;
    }

    public async Task<BookDTO?> GetBookByIsbnAsync(string isbn)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT b.book_id, b.isbn, b.title, b.description, b.publication_year,
                   b.publisher_id, b.language_id, b.series_id, b.cover_image_path,
                   COUNT(DISTINCT CASE WHEN bc.status = 'available' THEN bc.copy_id END) as available_copies,
                   COALESCE(b.default_copy_count, 0) as default_copy_count
            FROM Books b
            LEFT JOIN BookCopies bc ON b.book_id = bc.book_id
            WHERE b.isbn = @isbn
            GROUP BY b.book_id, b.isbn, b.title, b.description, b.publication_year,
                     b.publisher_id, b.language_id, b.series_id, b.cover_image_path, b.default_copy_count";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@isbn", isbn);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
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
                AvailableCopies = reader.GetInt32(9),
                CopiesCount = reader.IsDBNull(10) ? 0 : reader.GetInt32(10)
            };

            var (authorIds, authorNames) = await GetBookAuthorsAsync(bookId);
            var (genreIds, genreNames) = await GetBookGenresAsync(bookId);
            book.AuthorIds = authorIds;
            book.AuthorNames = authorNames;
            book.GenreIds = genreIds;
            book.GenreNames = genreNames;
            book.BranchAvailability = await GetBookBranchAvailabilityAsync(bookId);

            return book;
        }

        return null;
    }

    public async Task<Guid> CreateBookAsync(BookDTO book)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            var bookId = Guid.NewGuid();
            var sql = @"
                INSERT INTO Books (book_id, isbn, title, description, publication_year, 
                                  publisher_id, language_id, series_id, cover_image_path, default_copy_count)
                VALUES (@bookId, @isbn, @title, @description, @publicationYear,
                        @publisherId, @languageId, @seriesId, @coverImagePath, @defaultCopyCount)";

            using var cmd = new NpgsqlCommand(sql, conn, transaction);
            cmd.Parameters.AddWithValue("@bookId", bookId);
            cmd.Parameters.AddWithValue("@isbn", book.Isbn);
            cmd.Parameters.AddWithValue("@title", book.Title);
            cmd.Parameters.AddWithValue("@description", (object?)book.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@publicationYear", (object?)book.PublicationYear ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@publisherId", (object?)book.PublisherId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@languageId", (object?)book.LanguageId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@seriesId", (object?)book.SeriesId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@coverImagePath", (object?)book.CoverImagePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@defaultCopyCount", Math.Max(0, book.CopiesCount));

            await cmd.ExecuteNonQueryAsync();

            // Добавляем авторов
            foreach (var authorId in book.AuthorIds ?? new List<Guid>())
            {
                var authorSql = "INSERT INTO BookAuthors (book_id, author_id) VALUES (@bookId, @authorId) ON CONFLICT DO NOTHING";
                using var authorCmd = new NpgsqlCommand(authorSql, conn, transaction);
                authorCmd.Parameters.AddWithValue("@bookId", bookId);
                authorCmd.Parameters.AddWithValue("@authorId", authorId);
                await authorCmd.ExecuteNonQueryAsync();
            }

            // Добавляем жанры
            foreach (var genreId in book.GenreIds ?? new List<Guid>())
            {
                var genreSql = "INSERT INTO BookGenres (book_id, genre_id) VALUES (@bookId, @genreId) ON CONFLICT DO NOTHING";
                using var genreCmd = new NpgsqlCommand(genreSql, conn, transaction);
                genreCmd.Parameters.AddWithValue("@bookId", bookId);
                genreCmd.Parameters.AddWithValue("@genreId", genreId);
                await genreCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return bookId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateBookAsync(Guid bookId, BookDTO book)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            var sql = @"
                UPDATE Books 
                SET isbn = @isbn, title = @title, description = @description, 
                    publication_year = @publicationYear, publisher_id = @publisherId,
                    language_id = @languageId, series_id = @seriesId, 
                    cover_image_path = @coverImagePath,
                    default_copy_count = @defaultCopyCount
                WHERE book_id = @bookId";

            using var cmd = new NpgsqlCommand(sql, conn, transaction);
            cmd.Parameters.AddWithValue("@bookId", bookId);
            cmd.Parameters.AddWithValue("@isbn", book.Isbn);
            cmd.Parameters.AddWithValue("@title", book.Title);
            cmd.Parameters.AddWithValue("@description", (object?)book.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@publicationYear", (object?)book.PublicationYear ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@publisherId", (object?)book.PublisherId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@languageId", (object?)book.LanguageId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@seriesId", (object?)book.SeriesId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@coverImagePath", (object?)book.CoverImagePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@defaultCopyCount", Math.Max(0, book.CopiesCount));

            await cmd.ExecuteNonQueryAsync();

            // Удаляем старые связи
            var deleteAuthorsSql = "DELETE FROM BookAuthors WHERE book_id = @bookId";
            using var deleteAuthorsCmd = new NpgsqlCommand(deleteAuthorsSql, conn, transaction);
            deleteAuthorsCmd.Parameters.AddWithValue("@bookId", bookId);
            await deleteAuthorsCmd.ExecuteNonQueryAsync();

            var deleteGenresSql = "DELETE FROM BookGenres WHERE book_id = @bookId";
            using var deleteGenresCmd = new NpgsqlCommand(deleteGenresSql, conn, transaction);
            deleteGenresCmd.Parameters.AddWithValue("@bookId", bookId);
            await deleteGenresCmd.ExecuteNonQueryAsync();

            // Добавляем новые связи
            foreach (var authorId in book.AuthorIds ?? new List<Guid>())
            {
                var authorSql = "INSERT INTO BookAuthors (book_id, author_id) VALUES (@bookId, @authorId)";
                using var authorCmd = new NpgsqlCommand(authorSql, conn, transaction);
                authorCmd.Parameters.AddWithValue("@bookId", bookId);
                authorCmd.Parameters.AddWithValue("@authorId", authorId);
                await authorCmd.ExecuteNonQueryAsync();
            }

            foreach (var genreId in book.GenreIds ?? new List<Guid>())
            {
                var genreSql = "INSERT INTO BookGenres (book_id, genre_id) VALUES (@bookId, @genreId)";
                using var genreCmd = new NpgsqlCommand(genreSql, conn, transaction);
                genreCmd.Parameters.AddWithValue("@bookId", bookId);
                genreCmd.Parameters.AddWithValue("@genreId", genreId);
                await genreCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<int> AddCopiesAsync(Guid bookId, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            // Определяем основной филиал (первый в таблице Branches по имени)
            const string branchSql = "SELECT branch_id FROM Branches ORDER BY branch_name LIMIT 1";
            using var branchCmd = new NpgsqlCommand(branchSql, conn, transaction);
            var branchIdObj = await branchCmd.ExecuteScalarAsync();
            if (branchIdObj is not Guid branchId)
            {
                throw new InvalidOperationException("В системе не настроен ни один филиал (Branches).");
            }

            const string insertSql = @"
                INSERT INTO BookCopies (book_id, branch_id, status)
                VALUES (@bookId, @branchId, 'available')";

            using var insertCmd = new NpgsqlCommand(insertSql, conn, transaction);
            insertCmd.Parameters.AddWithValue("@bookId", bookId);
            insertCmd.Parameters.AddWithValue("@branchId", branchId);

            for (var i = 0; i < count; i++)
            {
                await insertCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return count;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteBookAsync(Guid bookId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = "DELETE FROM Books WHERE book_id = @bookId";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@bookId", bookId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<Guid?> GetAvailableCopyAsync(Guid bookId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT copy_id 
            FROM BookCopies 
            WHERE book_id = @bookId AND status = 'available' 
            LIMIT 1";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@bookId", bookId);

        var result = await cmd.ExecuteScalarAsync();
        return result != null ? (Guid?)result : null;
    }

    public async Task<bool> LoanBookAsync(Guid copyId, Guid userId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            // Обновляем статус копии
            var updateSql = "UPDATE BookCopies SET status = 'loaned' WHERE copy_id = @copyId";
            using var updateCmd = new NpgsqlCommand(updateSql, conn, transaction);
            updateCmd.Parameters.AddWithValue("@copyId", copyId);
            await updateCmd.ExecuteNonQueryAsync();

            // Создаем займ
            var loanDate = DateTime.UtcNow;
            var dueDate = loanDate.AddDays(14);
            var loanSql = @"
                INSERT INTO Loans (copy_id, user_id, loan_date, due_date)
                VALUES (@copyId, @userId, @loanDate, @dueDate)";

            using var loanCmd = new NpgsqlCommand(loanSql, conn, transaction);
            loanCmd.Parameters.AddWithValue("@copyId", copyId);
            loanCmd.Parameters.AddWithValue("@userId", userId);
            loanCmd.Parameters.AddWithValue("@loanDate", loanDate);
            loanCmd.Parameters.AddWithValue("@dueDate", dueDate);
            await loanCmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> ReturnBookAsync(Guid copyId, Guid userId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            // Находим активный займ
            var loanSql = @"
                SELECT loan_id 
                FROM Loans 
                WHERE copy_id = @copyId AND user_id = @userId AND return_date IS NULL
                ORDER BY loan_date DESC
                LIMIT 1";

            using var loanCmd = new NpgsqlCommand(loanSql, conn, transaction);
            loanCmd.Parameters.AddWithValue("@copyId", copyId);
            loanCmd.Parameters.AddWithValue("@userId", userId);

            var loanIdObj = await loanCmd.ExecuteScalarAsync();
            if (loanIdObj == null)
            {
                await transaction.RollbackAsync();
                return false;
            }

            var loanId = (Guid)loanIdObj;

            // Обновляем займ
            var updateLoanSql = "UPDATE Loans SET return_date = @returnDate WHERE loan_id = @loanId";
            using var updateLoanCmd = new NpgsqlCommand(updateLoanSql, conn, transaction);
            updateLoanCmd.Parameters.AddWithValue("@returnDate", DateTime.UtcNow);
            updateLoanCmd.Parameters.AddWithValue("@loanId", loanId);
            await updateLoanCmd.ExecuteNonQueryAsync();

            // Обновляем статус копии
            var updateCopySql = "UPDATE BookCopies SET status = 'available' WHERE copy_id = @copyId";
            using var updateCopyCmd = new NpgsqlCommand(updateCopySql, conn, transaction);
            updateCopyCmd.Parameters.AddWithValue("@copyId", copyId);
            await updateCopyCmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<(List<Guid> AuthorIds, List<string> AuthorNames)> GetBookAuthorsAsync(Guid bookId)
    {
        var authorIds = new List<Guid>();
        var authorNames = new List<string>();
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT a.author_id, a.first_name, a.last_name
            FROM BookAuthors ba
            INNER JOIN Authors a ON a.author_id = ba.author_id
            WHERE ba.book_id = @bookId
            ORDER BY a.last_name, a.first_name";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@bookId", bookId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var authorId = reader.GetGuid(0);
            authorIds.Add(authorId);
            var fullName = $"{reader.GetString(1)} {reader.GetString(2)}".Trim();
            authorNames.Add(fullName);
        }

        return (authorIds, authorNames);
    }

    public async Task<(List<Guid> GenreIds, List<string> GenreNames)> GetBookGenresAsync(Guid bookId)
    {
        var genreIds = new List<Guid>();
        var genreNames = new List<string>();
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT g.genre_id, g.genre_name
            FROM BookGenres bg
            INNER JOIN Genres g ON g.genre_id = bg.genre_id
            WHERE bg.book_id = @bookId
            ORDER BY g.genre_name";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@bookId", bookId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            genreIds.Add(reader.GetGuid(0));
            genreNames.Add(reader.GetString(1));
        }

        return (genreIds, genreNames);
    }

    public async Task<List<BranchAvailabilityDTO>> GetBookBranchAvailabilityAsync(Guid bookId)
    {
        var branches = new List<BranchAvailabilityDTO>();
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT br.branch_id, br.branch_name, br.address, br.city,
                   COUNT(bc.copy_id) AS total_copies,
                   COUNT(bc.copy_id) FILTER (WHERE bc.status = 'available') AS available_copies
            FROM Branches br
            INNER JOIN BookCopies bc ON bc.branch_id = br.branch_id
            WHERE bc.book_id = @bookId
            GROUP BY br.branch_id, br.branch_name, br.address, br.city
            ORDER BY br.branch_name";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@bookId", bookId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            branches.Add(new BranchAvailabilityDTO
            {
                BranchId = reader.GetGuid(0),
                BranchName = reader.GetString(1),
                Address = reader.IsDBNull(2) ? null : reader.GetString(2),
                City = reader.IsDBNull(3) ? null : reader.GetString(3),
                TotalCopies = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                AvailableCopies = reader.IsDBNull(5) ? 0 : reader.GetInt32(5)
            });
        }

        return branches;
    }
}

