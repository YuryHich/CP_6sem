using LibraryManagement.DTOs;

namespace LibraryManagement.Services;

public interface IUserService
{
    Task<List<LoanDTO>> GetUserLoansAsync(Guid userId);
}

