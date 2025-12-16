using LibraryManagement.DTOs;

namespace LibraryManagement.Services;

public interface IUserManagementService
{
    Task<List<UserManagementDTO>> GetAllUsersAsync();
    Task<bool> DeleteUserAsync(Guid userId);
    Task<bool> CanDeleteUserAsync(Guid userId);
}

