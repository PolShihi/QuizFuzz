namespace QuizFuzz.Web.Api.Services;

public sealed record AchievementDefinition(string Code, string Icon, Func<UserStatsSnapshot, int> Progress, int Target);

public sealed record UserStatsSnapshot(
    int GamesPlayed,
    int GamesWon,
    int QuestionsAnswered,
    int CorrectAnswers,
    int FirstCorrectAnswers,
    int BestAnswerTimeMs,
    int BestWinStreak,
    bool HasPerfectGame,
    bool HasActiveSubscription);

public static class AchievementCatalog
{
    public static readonly IReadOnlyList<AchievementDefinition> Definitions = new[]
    {
        new AchievementDefinition("FirstGame", "SportsEsports", s => Math.Min(s.GamesPlayed, 1), 1),
        new AchievementDefinition("FirstWin", "EmojiEvents", s => Math.Min(s.GamesWon, 1), 1),
        new AchievementDefinition("TenGames", "LocalFireDepartment", s => Math.Min(s.GamesPlayed, 10), 10),
        new AchievementDefinition("FirstCorrectAnswer", "CheckCircle", s => Math.Min(s.CorrectAnswers, 1), 1),
        new AchievementDefinition("TenCorrectAnswers", "Quiz", s => Math.Min(s.CorrectAnswers, 10), 10),
        new AchievementDefinition("FastAnswer", "Bolt", s => s.BestAnswerTimeMs > 0 && s.BestAnswerTimeMs <= 5000 ? 1 : 0, 1),
        new AchievementDefinition("PerfectGame", "Stars", s => s.HasPerfectGame ? 1 : 0, 1),
        new AchievementDefinition("WinStreak3", "MilitaryTech", s => Math.Min(s.BestWinStreak, 3), 3),
        new AchievementDefinition("PremiumSupporter", "WorkspacePremium", s => s.HasActiveSubscription ? 1 : 0, 1),
    };
}
