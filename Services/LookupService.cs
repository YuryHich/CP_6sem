using LibraryManagement.DataAccess;
using LibraryManagement.DTOs;

namespace LibraryManagement.Services;

public class LookupService : ILookupService
{
    private readonly LookupRepository _repository;

    public LookupService(DatabaseConnection db)
    {
        _repository = new LookupRepository(db);
    }

    public Task<List<GenreDTO>> GetGenresAsync() => _repository.GetGenresAsync();
    public Task<List<PublisherDTO>> GetPublishersAsync() => _repository.GetPublishersAsync();
    public Task<List<LanguageDTO>> GetLanguagesAsync() => _repository.GetLanguagesAsync();
    public Task<List<SeriesDTO>> GetSeriesAsync() => _repository.GetSeriesAsync();

    public Task<GenreDTO> CreateGenreAsync(GenreDTO genre) => _repository.CreateGenreAsync(genre);
    public Task UpdateGenreAsync(Guid genreId, GenreDTO genre) => _repository.UpdateGenreAsync(genreId, genre);
    public Task<bool> DeleteGenreAsync(Guid genreId) => _repository.DeleteGenreAsync(genreId);

    public Task<PublisherDTO> CreatePublisherAsync(PublisherDTO publisher) => _repository.CreatePublisherAsync(publisher);
    public Task UpdatePublisherAsync(Guid publisherId, PublisherDTO publisher) => _repository.UpdatePublisherAsync(publisherId, publisher);
    public Task<bool> DeletePublisherAsync(Guid publisherId) => _repository.DeletePublisherAsync(publisherId);

    public Task<LanguageDTO> CreateLanguageAsync(LanguageDTO language) => _repository.CreateLanguageAsync(language);
    public Task UpdateLanguageAsync(Guid languageId, LanguageDTO language) => _repository.UpdateLanguageAsync(languageId, language);
    public Task<bool> DeleteLanguageAsync(Guid languageId) => _repository.DeleteLanguageAsync(languageId);

    public Task<SeriesDTO> CreateSeriesAsync(SeriesDTO series) => _repository.CreateSeriesAsync(series);
}


