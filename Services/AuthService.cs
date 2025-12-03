using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using LibraryManagement.DataAccess;
using LibraryManagement.DTOs;
using LibraryManagement.Models;
using BCrypt.Net;

namespace LibraryManagement.Services;

public class AuthService : IAuthService
{
    private readonly UserRepository _userRepository;
    private readonly string _jwtKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public AuthService(DatabaseConnection db, IConfiguration configuration)
    {
        _userRepository = new UserRepository(db);
        _jwtKey = configuration["Jwt:Key"] ?? "YourSuperSecretKeyForJWTTokenGenerationThatIsAtLeast32Characters";
        _jwtIssuer = configuration["Jwt:Issuer"] ?? "LibraryManagement";
        _jwtAudience = configuration["Jwt:Audience"] ?? "LibraryManagementUsers";
    }

    public async Task<AuthResponseDTO?> LoginAsync(LoginDTO loginDto)
    {
        var user = await _userRepository.GetUserByUsernameAsync(loginDto.Username);
        if (user == null || !user.IsActive)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        {
            return null;
        }

        var role = await _userRepository.GetUserRoleAsync(user.UserId);
        var token = GenerateJwtToken(user.UserId, user.Username, role);

        return new AuthResponseDTO
        {
            Token = token,
            UserId = user.UserId,
            Username = user.Username,
            Role = role
        };
    }

    public async Task<AuthResponseDTO> RegisterAsync(RegisterDTO registerDto)
    {
        // Проверяем, существует ли пользователь
        var existingUser = await _userRepository.GetUserByUsernameAsync(registerDto.Username);
        if (existingUser != null)
        {
            throw new Exception("Username already exists");
        }

        var existingEmail = await _userRepository.GetUserByEmailAsync(registerDto.Email);
        if (existingEmail != null)
        {
            throw new Exception("Email already exists");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);
        var userId = await _userRepository.CreateUserAsync(registerDto, passwordHash);

        var token = GenerateJwtToken(userId, registerDto.Username, "user");

        return new AuthResponseDTO
        {
            Token = token,
            UserId = userId,
            Username = registerDto.Username,
            Role = "user"
        };
    }

    private string GenerateJwtToken(Guid userId, string username, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

