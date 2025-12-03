using LibraryManagement.DataAccess;
using LibraryManagement.DTOs;

namespace LibraryManagement.Services;

public class AuthorService : IAuthorService
{
    private readonly AuthorRepository _repository;

    public AuthorService(DatabaseConnection db)
    {
        _repository = new AuthorRepository(db);
    }

    public async Task<List<AuthorDTO>> GetAuthorsAsync()
    {
        return await _repository.GetAuthorsAsync();
    }

    public async Task<AuthorDTO?> GetAuthorByIdAsync(Guid authorId)
    {
        return await _repository.GetAuthorByIdAsync(authorId);
    }

    public async Task<Guid> CreateAuthorAsync(AuthorDTO author)
    {
        return await _repository.CreateAuthorAsync(author);
    }

    public async Task UpdateAuthorAsync(Guid authorId, AuthorDTO author)
    {
        await _repository.UpdateAuthorAsync(authorId, author);
    }

    public async Task<bool> DeleteAuthorAsync(Guid authorId)
    {
        return await _repository.DeleteAuthorAsync(authorId);
    }

    public async Task<List<BookDTO>> GetAuthorBooksAsync(Guid authorId)
    {
        return await _repository.GetAuthorBooksAsync(authorId);
    }
}

