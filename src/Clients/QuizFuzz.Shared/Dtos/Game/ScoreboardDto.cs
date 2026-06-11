namespace QuizFuzz.Shared.Dtos.Game;

public class ScoreboardDto
{
    public Guid SessionId { get; set; }
    public List<PlayerScoreDto> Players { get; set; } = new();
}

public class PlayerScoreDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int TotalScore { get; set; }
    public int CorrectAnswers { get; set; }
    public int UniqueCorrectAnswers { get; set; }
    public bool HasActiveSubscription { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
}
