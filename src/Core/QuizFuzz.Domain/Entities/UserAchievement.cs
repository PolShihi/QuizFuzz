using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Полученное пользователем достижение.
/// Определения достижений хранятся в коде, а в БД фиксируется факт получения.
/// </summary>
public class UserAchievement : BaseEntity
{
    public Guid UserId { get; private set; }
    public string AchievementCode { get; private set; } = string.Empty;
    public DateTime UnlockedAt { get; private set; }

    public User User { get; private set; } = null!;

    private UserAchievement() { } // EF Core

    public UserAchievement(Guid userId, string achievementCode, DateTime unlockedAt)
    {
        if (string.IsNullOrWhiteSpace(achievementCode))
            throw new ArgumentException("Achievement code cannot be empty", nameof(achievementCode));

        UserId = userId;
        AchievementCode = achievementCode.Trim();
        UnlockedAt = unlockedAt;
    }
}
