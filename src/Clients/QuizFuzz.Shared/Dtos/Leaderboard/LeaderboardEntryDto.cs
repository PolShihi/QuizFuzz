namespace QuizFuzz.Shared.Dtos.Leaderboard;

public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int TotalScore { get; set; }
    public int GamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public int AnsweredRounds { get; set; }
    public int CorrectRounds { get; set; }
    public double Accuracy { get; set; }
    public bool HasActiveSubscription { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
}
