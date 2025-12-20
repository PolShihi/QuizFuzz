using QuizFuzz.Domain.Common;
using QuizFuzz.Domain.ValueObjects;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Таблица очков для сессии
/// </summary>
public class Scoreboard : BaseEntity
{
    public Guid SessionId { get; private set; }
    public Guid UserId { get; private set; }
    public int ScoreTotal { get; private set; }
    public int CorrectCount { get; private set; }
    public int UniqueCorrectCount { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public GameSession Session { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private Scoreboard() { } // EF Core

    public Scoreboard(Guid sessionId, Guid userId)
    {
        SessionId = sessionId;
        UserId = userId;
        ScoreTotal = 0;
        CorrectCount = 0;
        UniqueCorrectCount = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddScore(int points, bool isCorrect, bool isFirstCorrect = false)
    {
        if (points < 0)
            throw new ArgumentException("Points cannot be negative", nameof(points));

        ScoreTotal += points;

        if (isCorrect)
        {
            CorrectCount++;

            if (isFirstCorrect)
            {
                UniqueCorrectCount++;
            }
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void AddScore(Score score, bool isCorrect, bool isFirstCorrect = false)
    {
        AddScore(score.Value, isCorrect, isFirstCorrect);
    }

    public Score GetScore() => Score.Create(ScoreTotal);

    public void Reset()
    {
        ScoreTotal = 0;
        CorrectCount = 0;
        UniqueCorrectCount = 0;
        UpdatedAt = DateTime.UtcNow;
    }
}
