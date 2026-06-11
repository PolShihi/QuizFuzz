using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuizFuzz.Application.Common.Interfaces.Services;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace QuizFuzz.Infrastructure.Services.Email;

public class EmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<EmailOptions> options, ILogger<EmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailVerificationCodeAsync(
        string email,
        string username,
        string code,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(_options.Mode, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("[DEV EMAIL] QuizFuzz verification code for {Email} ({Username}): {Code}. Expires at {ExpiresAt:u}",
                email,
                username,
                code,
                expiresAt);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.SmtpHost))
            throw new InvalidOperationException("SMTP host is not configured.");

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName, Encoding.UTF8),
            Subject = "QuizFuzz verification code",
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = false,
            Body = $"Hello, {username}!\n\nYour QuizFuzz verification code is: {code}\n\nThe code is valid until {expiresAt:u}.\n\nIf you did not request this registration, ignore this email."
        };

        message.To.Add(email);

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_options.SmtpUsername))
        {
            client.Credentials = new NetworkCredential(_options.SmtpUsername, _options.SmtpPassword);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }
}
