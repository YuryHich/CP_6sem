using LibraryManagement.DataAccess;
using LibraryManagement.DTOs;

namespace LibraryManagement.Services;

public class UserService : IUserService
{
    private readonly UserRepository _repository;

    public UserService(DatabaseConnection db)
    {
        _repository = new UserRepository(db);
    }

    public async Task<List<LoanDTO>> GetUserLoansAsync(Guid userId)
    {
        return await _repository.GetUserLoansAsync(userId);
    }
}

