namespace LibraryManagement.Services;

public interface IEmailService
{
    Task SendEmailConfirmationAsync(string email, string token, string username);
    Task SendPasswordResetAsync(string email, string token, string username);
}

