using LibraryManagement.DTOs;

namespace LibraryManagement.Services;

public interface IBookService
{
    Task<PagedResult<BookDTO>> GetBooksAsync(int page, int size, string? search, Guid? genreId, Guid? authorId);
    Task<BookDTO?> GetBookByIdAsync(Guid bookId);
    Task<BookDTO?> GetBookByIsbnAsync(string isbn);
    Task<Guid> CreateBookAsync(BookDTO book);
    Task<int> UpdateBookAsync(Guid bookId, BookDTO book);
    Task DeleteBookAsync(Guid bookId);
    Task<bool> LoanBookAsync(Guid bookId, Guid userId);
    Task<bool> ReturnBookAsync(Guid bookId, Guid userId);
}

