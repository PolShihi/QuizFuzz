using QuizFuzz.Domain.Common;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Пользовательская подписка. Сейчас используется demo-источник, позже сюда можно привязать платежный webhook.
/// </summary>
public class UserSubscription : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public string PlanCode { get; private set; } = string.Empty;
    public UserSubscriptionStatus Status { get; private set; }
    public string Source { get; private set; } = string.Empty;

    public DateTime StartedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    private UserSubscription() { }

    public UserSubscription(
        Guid userId,
        string planCode,
        DateTime startedAt,
        DateTime expiresAt,
        string source = "Demo")
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty", nameof(userId));

        if (string.IsNullOrWhiteSpace(planCode))
            throw new ArgumentException("Plan code cannot be empty", nameof(planCode));

        if (expiresAt <= startedAt)
            throw new ArgumentException("Subscription expiration must be after start date", nameof(expiresAt));

        UserId = userId;
        PlanCode = planCode.Trim();
        StartedAt = startedAt;
        ExpiresAt = expiresAt;
        Source = string.IsNullOrWhiteSpace(source) ? "Demo" : source.Trim();
        Status = UserSubscriptionStatus.Active;
    }

    public bool IsActive(DateTime utcNow)
        => Status == UserSubscriptionStatus.Active && ExpiresAt > utcNow;

    public void ExtendTo(DateTime newExpiresAt)
    {
        if (newExpiresAt <= ExpiresAt)
            throw new ArgumentException("New expiration must be later than current expiration", nameof(newExpiresAt));

        ExpiresAt = newExpiresAt;
        Status = UserSubscriptionStatus.Active;
        CancelledAt = null;
    }

    public void Cancel(DateTime utcNow)
    {
        if (Status == UserSubscriptionStatus.Cancelled)
            return;

        Status = UserSubscriptionStatus.Cancelled;
        CancelledAt = utcNow;
    }

    public void MarkExpired()
    {
        if (Status == UserSubscriptionStatus.Active)
            Status = UserSubscriptionStatus.Expired;
    }
}
