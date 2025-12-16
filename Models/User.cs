namespace LibraryManagement.Models;

public class User
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime RegistrationDate { get; set; }
    public Guid RoleId { get; set; }
    public bool IsActive { get; set; }
    public string? ConfirmationToken { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? ResetTokenExpiration { get; set; }
}

