using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace LibraryManagement.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _baseUrl;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _enableSsl;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _baseUrl = configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";
        
        // Читаем настройки email из конфигурации
        _smtpHost = configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(configuration["EmailSettings:SmtpPort"] ?? "587");
        _smtpUsername = configuration["EmailSettings:SmtpUsername"] ?? "";
        _smtpPassword = configuration["EmailSettings:SmtpPassword"] ?? "";
        _fromEmail = configuration["EmailSettings:FromEmail"] ?? "";
        _fromName = configuration["EmailSettings:FromName"] ?? "Система управления библиотекой";
        _enableSsl = bool.Parse(configuration["EmailSettings:EnableSsl"] ?? "true");
    }

    public async Task SendEmailConfirmationAsync(string email, string token, string username)
    {
        var confirmationUrl = $"{_baseUrl}/Account/ConfirmEmail?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";
        
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Добро пожаловать в библиотеку!</h1>
        </div>
        <div class=""content"">
            <p>Здравствуйте, {username}!</p>
            <p>Спасибо за регистрацию в нашей системе управления библиотекой.</p>
            <p>Для завершения регистрации необходимо подтвердить ваш email адрес.</p>
            <p style=""text-align: center;"">
                <a href=""{confirmationUrl}"" class=""button"">Подтвердить email</a>
            </p>
            <p>Или скопируйте и вставьте следующую ссылку в браузер:</p>
            <p style=""word-break: break-all; color: #007bff;"">{confirmationUrl}</p>
            <p><strong>Важно:</strong> Ссылка действительна в течение 24 часов.</p>
        </div>
        <div class=""footer"">
            <p>Если вы не регистрировались в нашей системе, просто проигнорируйте это письмо.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, "Подтверждение email", htmlBody);
    }

    public async Task SendPasswordResetAsync(string email, string token, string username)
    {
        var resetUrl = $"{_baseUrl}/Account/ResetPassword?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";
        
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #dc3545; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Сброс пароля</h1>
        </div>
        <div class=""content"">
            <p>Здравствуйте, {username}!</p>
            <p>Вы запросили сброс пароля для вашего аккаунта.</p>
            <p style=""text-align: center;"">
                <a href=""{resetUrl}"" class=""button"">Сбросить пароль</a>
            </p>
            <p>Или скопируйте и вставьте следующую ссылку в браузер:</p>
            <p style=""word-break: break-all; color: #dc3545;"">{resetUrl}</p>
            <div class=""warning"">
                <p><strong>Важно:</strong> Ссылка действительна в течение 1 часа.</p>
                <p>Если вы не запрашивали сброс пароля, просто проигнорируйте это письмо.</p>
            </div>
        </div>
        <div class=""footer"">
            <p>Система управления библиотекой</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, "Сброс пароля", htmlBody);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            // Создаем сообщение
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _fromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            // Создаем HTML тело письма
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            // Отправляем через SMTP
            using var client = new SmtpClient();
            
            _logger.LogInformation("Подключение к SMTP серверу {Host}:{Port}", _smtpHost, _smtpPort);
            await client.ConnectAsync(_smtpHost, _smtpPort, _enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            
            _logger.LogInformation("Аутентификация на SMTP сервере");
            await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
            
            _logger.LogInformation("Отправка письма на {Email}", toEmail);
            await client.SendAsync(message);
            
            await client.DisconnectAsync(true);
            
            _logger.LogInformation("Письмо успешно отправлено на {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке письма на {Email}: {Message}", toEmail, ex.Message);
            throw new Exception($"Не удалось отправить письмо: {ex.Message}", ex);
        }
    }
}
