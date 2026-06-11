namespace QuizFuzz.Shared.Dtos.Users;

public class UserStatsDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public int GamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public int GamesLost { get; set; }

    public int TotalScore { get; set; }
    public double AverageScorePerGame { get; set; }
    public int BestGameScore { get; set; }

    public int QuestionsAnswered { get; set; }
    public int AnswerAttempts { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public int FirstCorrectAnswers { get; set; }
    public int UniqueFirstCorrect { get; set; }

    public double WinRate { get; set; }
    public double Accuracy { get; set; }

    public int AverageAnswerTimeMs { get; set; }
    public int BestAnswerTimeMs { get; set; }

    public int CurrentWinStreak { get; set; }
    public int BestWinStreak { get; set; }
    public DateTime? LastPlayedAt { get; set; }

    public bool HasActiveSubscription { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }

    public List<UserTagStatDto> FavoriteTags { get; set; } = new();
    public List<UserTagStatDto> StrongTags { get; set; } = new();
    public List<UserTagStatDto> WeakTags { get; set; } = new();
    public AuthorQuestionStatsDto AuthorStats { get; set; } = new();
    public List<UserAchievementDto> Achievements { get; set; } = new();
    public List<GameHistoryItemDto> RecentGames { get; set; } = new();
}
