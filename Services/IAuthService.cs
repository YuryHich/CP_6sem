using LibraryManagement.DTOs;

namespace LibraryManagement.Services;

public interface IAuthService
{
    Task<AuthResponseDTO?> LoginAsync(LoginDTO loginDto);
    Task<RegisterResponseDTO> RegisterAsync(RegisterDTO registerDto);
    Task<bool> ConfirmEmailAsync(string email, string token);
    Task<bool> RequestPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
}

