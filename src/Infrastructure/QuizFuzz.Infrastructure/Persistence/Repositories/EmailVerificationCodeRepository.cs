using Microsoft.EntityFrameworkCore;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Repositories;

public class EmailVerificationCodeRepository : IEmailVerificationCodeRepository
{
    private readonly ApplicationDbContext _context;

    public EmailVerificationCodeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(EmailVerificationCode verificationCode, CancellationToken cancellationToken = default)
    {
        await _context.Set<EmailVerificationCode>().AddAsync(verificationCode, cancellationToken);
    }

    public async Task<EmailVerificationCode?> GetLatestActiveByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        return await _context.Set<EmailVerificationCode>()
            .Where(x => x.Email == normalizedEmail && x.ConfirmedAt == null && x.ExpiresAt > now)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasActiveUsernameAsync(
        string username,
        string? excludingEmail = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.Trim().ToLowerInvariant();
        var normalizedEmail = excludingEmail?.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        return await _context.Set<EmailVerificationCode>()
            .AnyAsync(x =>
                x.Username.ToLower() == normalizedUsername &&
                x.ConfirmedAt == null &&
                x.ExpiresAt > now &&
                (normalizedEmail == null || x.Email != normalizedEmail),
                cancellationToken);
    }

    public async Task<IReadOnlyList<EmailVerificationCode>> GetExpiredUnconfirmedAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<EmailVerificationCode>()
            .Where(x => x.ConfirmedAt == null && x.ExpiresAt <= utcNow)
            .ToListAsync(cancellationToken);
    }

    public void RemoveRange(IEnumerable<EmailVerificationCode> verificationCodes)
    {
        _context.Set<EmailVerificationCode>().RemoveRange(verificationCodes);
    }
}
