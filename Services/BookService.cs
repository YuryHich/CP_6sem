using LibraryManagement.DataAccess;
using LibraryManagement.DTOs;

namespace LibraryManagement.Services;

public class BookService : IBookService
{
    private readonly BookRepository _repository;
    private readonly DatabaseConnection _db;

    public BookService(DatabaseConnection db)
    {
        _db = db;
        _repository = new BookRepository(db);
    }

    public async Task<PagedResult<BookDTO>> GetBooksAsync(int page, int size, string? search, Guid? genreId, Guid? authorId)
    {
        return await _repository.GetBooksAsync(page, size, search, genreId, authorId);
    }

    public async Task<BookDTO?> GetBookByIdAsync(Guid bookId)
    {
        return await _repository.GetBookByIdAsync(bookId);
    }

    public async Task<BookDTO?> GetBookByIsbnAsync(string isbn)
    {
        return await _repository.GetBookByIsbnAsync(isbn);
    }

    public async Task<Guid> CreateBookAsync(BookDTO book)
    {
        return await _repository.CreateBookAsync(book);
    }

    public async Task UpdateBookAsync(Guid bookId, BookDTO book)
    {
        await _repository.UpdateBookAsync(bookId, book);
    }

    public async Task DeleteBookAsync(Guid bookId)
    {
        await _repository.DeleteBookAsync(bookId);
    }

    public async Task<bool> LoanBookAsync(Guid bookId, Guid userId)
    {
        var copyId = await _repository.GetAvailableCopyAsync(bookId);
        if (copyId == null)
        {
            return false;
        }

        return await _repository.LoanBookAsync(copyId.Value, userId);
    }

    public async Task<bool> ReturnBookAsync(Guid bookId, Guid userId)
    {
        // Находим активный займ пользователя для этой книги
        var userRepo = new UserRepository(_db);
        var loans = await userRepo.GetUserLoansAsync(userId);
        var activeLoan = loans.FirstOrDefault(l => l.BookId == bookId && l.ReturnDate == null);
        
        if (activeLoan == null)
        {
            return false;
        }

        // Находим copy_id по loan_id
        var copyId = await userRepo.GetCopyIdByLoanIdAsync(activeLoan.LoanId);
        if (copyId == null)
        {
            return false;
        }

        return await _repository.ReturnBookAsync(copyId.Value, userId);
    }
}

