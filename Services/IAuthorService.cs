using LibraryManagement.DTOs;

namespace LibraryManagement.Services;

public interface IAuthorService
{
    Task<List<AuthorDTO>> GetAuthorsAsync();
    Task<AuthorDTO?> GetAuthorByIdAsync(Guid authorId);
    Task<Guid> CreateAuthorAsync(AuthorDTO author);
    Task UpdateAuthorAsync(Guid authorId, AuthorDTO author);
    Task<bool> DeleteAuthorAsync(Guid authorId);
    Task<List<BookDTO>> GetAuthorBooksAsync(Guid authorId);
}

