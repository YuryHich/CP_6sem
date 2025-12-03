using LibraryManagement.DTOs;
using Npgsql;

namespace LibraryManagement.DataAccess;

public class LookupRepository
{
    private readonly DatabaseConnection _db;

    public LookupRepository(DatabaseConnection db)
    {
        _db = db;
    }

    public async Task<List<GenreDTO>> GetGenresAsync()
    {
        var genres = new List<GenreDTO>();
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        const string sql = "SELECT genre_id, genre_name FROM Genres ORDER BY genre_name";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            genres.Add(new GenreDTO
            {
                GenreId = reader.GetGuid(0),
                GenreName = reader.GetString(1)
            });
        }

        return genres;
    }

    public async Task<List<PublisherDTO>> GetPublishersAsync()
    {
        var publishers = new List<PublisherDTO>();
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        const string sql = "SELECT publisher_id, publisher_name, country FROM Publishers ORDER BY publisher_name";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            publishers.Add(new PublisherDTO
            {
                PublisherId = reader.GetGuid(0),
                PublisherName = reader.GetString(1),
                Country = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        }

        return publishers;
    }

    public async Task<List<LanguageDTO>> GetLanguagesAsync()
    {
        var languages = new List<LanguageDTO>();
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        const string sql = "SELECT language_id, language_name FROM Languages ORDER BY language_name";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            languages.Add(new LanguageDTO
            {
                LanguageId = reader.GetGuid(0),
                LanguageName = reader.GetString(1)
            });
        }

        return languages;
    }

    public async Task<List<SeriesDTO>> GetSeriesAsync()
    {
        var series = new List<SeriesDTO>();
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        const string sql = "SELECT series_id, series_name, description FROM Series ORDER BY series_name";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            series.Add(new SeriesDTO
            {
                SeriesId = reader.GetGuid(0),
                SeriesName = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        }

        return series;
    }

    public async Task<GenreDTO> CreateGenreAsync(GenreDTO genre)
    {
        var genreId = Guid.NewGuid();
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        const string sql = "INSERT INTO Genres (genre_id, genre_name) VALUES (@genreId, @genreName)";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@genreId", genreId);
        cmd.Parameters.AddWithValue("@genreName", genre.GenreName);
        await cmd.ExecuteNonQueryAsync();

        return new GenreDTO { GenreId = genreId, GenreName = genre.GenreName };
    }

    public async Task<PublisherDTO> CreatePublisherAsync(PublisherDTO publisher)
    {
        var publisherId = Guid.NewGuid();
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        const string sql = "INSERT INTO Publishers (publisher_id, publisher_name, country) VALUES (@publisherId, @publisherName, @country)";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@publisherId", publisherId);
        cmd.Parameters.AddWithValue("@publisherName", publisher.PublisherName);
        cmd.Parameters.AddWithValue("@country", (object?)publisher.Country ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();

        return new PublisherDTO { PublisherId = publisherId, PublisherName = publisher.PublisherName, Country = publisher.Country };
    }

    public async Task<LanguageDTO> CreateLanguageAsync(LanguageDTO language)
    {
        var languageId = Guid.NewGuid();
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        const string sql = "INSERT INTO Languages (language_id, language_name) VALUES (@languageId, @languageName)";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@languageId", languageId);
        cmd.Parameters.AddWithValue("@languageName", language.LanguageName);
        await cmd.ExecuteNonQueryAsync();

        return new LanguageDTO { LanguageId = languageId, LanguageName = language.LanguageName };
    }

    public async Task<SeriesDTO> CreateSeriesAsync(SeriesDTO series)
    {
        var seriesId = Guid.NewGuid();
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        const string sql = "INSERT INTO Series (series_id, series_name, description) VALUES (@seriesId, @seriesName, @description)";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@seriesId", seriesId);
        cmd.Parameters.AddWithValue("@seriesName", series.SeriesName);
        cmd.Parameters.AddWithValue("@description", (object?)series.Description ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();

        return new SeriesDTO { SeriesId = seriesId, SeriesName = series.SeriesName, Description = series.Description };
    }
}


