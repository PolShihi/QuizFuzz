using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Materialized leaderboard row for a selected period and metric.
/// The table is rebuilt from finished game sessions and scoreboards.
/// </summary>
public class LeaderboardEntry : BaseEntity
{
    public string Period { get; private set; } = string.Empty;
    public string Metric { get; private set; } = string.Empty;
    public DateTime? PeriodStart { get; private set; }
    public DateTime? PeriodEnd { get; private set; }
    public Guid UserId { get; private set; }
    public int Rank { get; private set; }
    public int TotalScore { get; private set; }
    public int GamesPlayed { get; private set; }
    public int GamesWon { get; private set; }
    public int AnsweredRounds { get; private set; }
    public int CorrectRounds { get; private set; }
    public decimal Accuracy { get; private set; }
    public DateTime CalculatedAt { get; private set; }

    public User User { get; private set; } = null!;

    private LeaderboardEntry() { }

    public LeaderboardEntry(
        string period,
        string metric,
        DateTime? periodStart,
        DateTime? periodEnd,
        Guid userId,
        int rank,
        int totalScore,
        int gamesPlayed,
        int gamesWon,
        int answeredRounds,
        int correctRounds,
        decimal accuracy,
        DateTime calculatedAt)
    {
        if (string.IsNullOrWhiteSpace(period))
            throw new ArgumentException("Period cannot be empty", nameof(period));

        if (string.IsNullOrWhiteSpace(metric))
            throw new ArgumentException("Metric cannot be empty", nameof(metric));

        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty", nameof(userId));

        Period = period.Trim();
        Metric = metric.Trim();
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        UserId = userId;
        Rank = rank;
        TotalScore = totalScore;
        GamesPlayed = gamesPlayed;
        GamesWon = gamesWon;
        AnsweredRounds = answeredRounds;
        CorrectRounds = correctRounds;
        Accuracy = Math.Clamp(accuracy, 0m, 100m);
        CalculatedAt = calculatedAt;
    }
}
