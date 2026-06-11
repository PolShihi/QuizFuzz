namespace QuizFuzz.Application.Common.Interfaces.Services;

public interface IEmailSender
{
    Task SendEmailVerificationCodeAsync(
        string email,
        string username,
        string code,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);
}
