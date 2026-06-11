using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Application.Common.Interfaces.Persistence;

public interface IEmailVerificationCodeRepository
{
    Task AddAsync(EmailVerificationCode verificationCode, CancellationToken cancellationToken = default);
    Task<EmailVerificationCode?> GetLatestActiveByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> HasActiveUsernameAsync(string username, string? excludingEmail = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailVerificationCode>> GetExpiredUnconfirmedAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    void RemoveRange(IEnumerable<EmailVerificationCode> verificationCodes);
}
