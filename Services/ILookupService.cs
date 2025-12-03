using LibraryManagement.DTOs;

namespace LibraryManagement.Services;

public interface ILookupService
{
    Task<List<GenreDTO>> GetGenresAsync();
    Task<List<PublisherDTO>> GetPublishersAsync();
    Task<List<LanguageDTO>> GetLanguagesAsync();
    Task<List<SeriesDTO>> GetSeriesAsync();
    Task<GenreDTO> CreateGenreAsync(GenreDTO genre);
    Task<PublisherDTO> CreatePublisherAsync(PublisherDTO publisher);
    Task<LanguageDTO> CreateLanguageAsync(LanguageDTO language);
    Task<SeriesDTO> CreateSeriesAsync(SeriesDTO series);
}


