namespace QuizFuzz.Shared.Dtos.Users;

public class UserAchievementDto
{
    public string Code { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsUnlocked { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public int Progress { get; set; }
    public int Target { get; set; }
}
