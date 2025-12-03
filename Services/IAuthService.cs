using LibraryManagement.DTOs;

namespace LibraryManagement.Services;

public interface IAuthService
{
    Task<AuthResponseDTO?> LoginAsync(LoginDTO loginDto);
    Task<AuthResponseDTO> RegisterAsync(RegisterDTO registerDto);
}

