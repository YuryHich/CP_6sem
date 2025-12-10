using LibraryManagement.DTOs;

namespace LibraryManagement.Services;

public interface ILookupService
{
    Task<List<GenreDTO>> GetGenresAsync();
    Task<List<PublisherDTO>> GetPublishersAsync();
    Task<List<LanguageDTO>> GetLanguagesAsync();
    Task<List<SeriesDTO>> GetSeriesAsync();
    Task<GenreDTO> CreateGenreAsync(GenreDTO genre);
    Task UpdateGenreAsync(Guid genreId, GenreDTO genre);
    Task<bool> DeleteGenreAsync(Guid genreId);
    Task<PublisherDTO> CreatePublisherAsync(PublisherDTO publisher);
    Task UpdatePublisherAsync(Guid publisherId, PublisherDTO publisher);
    Task<bool> DeletePublisherAsync(Guid publisherId);
    Task<LanguageDTO> CreateLanguageAsync(LanguageDTO language);
    Task UpdateLanguageAsync(Guid languageId, LanguageDTO language);
    Task<bool> DeleteLanguageAsync(Guid languageId);
    Task<SeriesDTO> CreateSeriesAsync(SeriesDTO series);
}


