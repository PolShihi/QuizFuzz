using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Серверная refresh-сессия пользователя.
/// В базе хранится только хэш refresh token, сам секрет известен только клиенту.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? UserAgent { get; private set; }

    private RefreshToken() { } // EF Core

    public RefreshToken(
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        string? createdByIp = null,
        string? userAgent = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty", nameof(userId));

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash cannot be empty", nameof(tokenHash));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration date must be in the future", nameof(expiresAt));

        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedByIp = createdByIp;
        UserAgent = userAgent;
    }

    public bool IsExpired(DateTime? utcNow = null) => (utcNow ?? DateTime.UtcNow) >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive(DateTime? utcNow = null) => !IsRevoked && !IsExpired(utcNow);

    public void Revoke(string? revokedByIp = null, string? replacedByTokenHash = null)
    {
        if (IsRevoked)
            return;

        RevokedAt = DateTime.UtcNow;
        RevokedByIp = revokedByIp;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
