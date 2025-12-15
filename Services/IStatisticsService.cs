using LibraryManagement.DTOs;

namespace LibraryManagement.Services;

public interface IStatisticsService
{
    Task<GlobalStatisticsDTO> GetGlobalStatisticsAsync();
    Task<UserStatisticsDTO> GetUserStatisticsAsync(Guid userId);
    Task<List<AuthorStatisticsDTO>> GetAuthorStatisticsAsync();
    Task<List<GenreStatisticsDTO>> GetGenreStatisticsAsync();
    Task<List<LanguageStatisticsDTO>> GetLanguageStatisticsAsync();
}

