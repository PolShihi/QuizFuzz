using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Временная регистрация пользователя с одноразовым кодом подтверждения email.
/// Пароль и код хранятся только в виде хэшей.
/// </summary>
public class EmailVerificationCode : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string CodeHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public int AttemptsCount { get; private set; }
    public int ResendCount { get; private set; }
    public DateTime LastSentAt { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? UserAgent { get; private set; }

    private EmailVerificationCode() { } // EF Core

    public EmailVerificationCode(
        string email,
        string username,
        string passwordHash,
        string codeHash,
        DateTime expiresAt,
        string? createdByIp = null,
        string? userAgent = null)
    {
        SetRegistrationData(email, username, passwordHash);
        SetCode(codeHash, expiresAt);
        CreatedByIp = createdByIp;
        UserAgent = userAgent;
    }

    public bool IsExpired(DateTime? utcNow = null) => (utcNow ?? DateTime.UtcNow) >= ExpiresAt;
    public bool IsConfirmed => ConfirmedAt.HasValue;
    public bool IsActive(DateTime? utcNow = null) => !IsConfirmed && !IsExpired(utcNow);

    public void ReplacePendingRegistration(
        string username,
        string passwordHash,
        string codeHash,
        DateTime expiresAt,
        string? userAgent = null)
    {
        if (IsConfirmed)
            throw new InvalidOperationException("Confirmed verification request cannot be reused.");

        SetRegistrationData(Email, username, passwordHash);
        SetCode(codeHash, expiresAt);
        AttemptsCount = 0;
        ResendCount++;
        UserAgent = userAgent ?? UserAgent;
    }

    public void RefreshCode(string codeHash, DateTime expiresAt)
    {
        if (IsConfirmed)
            throw new InvalidOperationException("Confirmed verification request cannot be refreshed.");

        SetCode(codeHash, expiresAt);
        AttemptsCount = 0;
        ResendCount++;
    }

    public void RegisterFailedAttempt()
    {
        AttemptsCount++;
    }

    public void Confirm()
    {
        if (IsConfirmed)
            return;

        ConfirmedAt = DateTime.UtcNow;
    }

    private void SetRegistrationData(string email, string username, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        Email = email.Trim().ToLowerInvariant();
        Username = username.Trim();
        PasswordHash = passwordHash;
    }

    private void SetCode(string codeHash, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(codeHash))
            throw new ArgumentException("Code hash cannot be empty", nameof(codeHash));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration date must be in the future", nameof(expiresAt));

        CodeHash = codeHash;
        ExpiresAt = expiresAt;
        LastSentAt = DateTime.UtcNow;
    }
}
