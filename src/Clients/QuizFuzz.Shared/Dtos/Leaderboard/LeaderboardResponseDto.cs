namespace QuizFuzz.Shared.Dtos.Leaderboard;

public class LeaderboardResponseDto
{
    public string Period { get; set; } = "Global";
    public string Metric { get; set; } = "Score";
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public DateTime CalculatedAt { get; set; }
    public int TotalPlayers { get; set; }
    public LeaderboardEntryDto? CurrentUserEntry { get; set; }
    public List<LeaderboardEntryDto> Entries { get; set; } = new();
}
