using QuizFuzz.Domain.Common;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Игровой раунд
/// </summary>
public class GameRound : BaseEntity
{
    public Guid SessionId { get; private set; }
    public int RoundIndex { get; private set; }
    public Guid QuestionId { get; private set; }
    public RoundStatus Status { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public int TimeLimitSec { get; private set; }
    public Guid? WinnerUserId { get; private set; }

    // Navigation properties
    public GameSession Session { get; private set; } = null!;
    public Question Question { get; private set; } = null!;
    public User? Winner { get; private set; }

    private readonly List<PlayerAnswer> _answers = new();
    public IReadOnlyCollection<PlayerAnswer> Answers => _answers.AsReadOnly();

    private GameRound() { } // EF Core

    public GameRound(Guid sessionId, int roundIndex, Guid questionId, int timeLimitSec = 60)
    {
        if (timeLimitSec < 10 || timeLimitSec > 300)
            throw new ArgumentException("Time limit must be between 10 and 300 seconds", nameof(timeLimitSec));

        SessionId = sessionId;
        RoundIndex = roundIndex;
        QuestionId = questionId;
        TimeLimitSec = timeLimitSec;
        Status = RoundStatus.Pending;
    }

    public void Start()
    {
        if (Status != RoundStatus.Pending)
            throw new InvalidOperationException("Can only start a pending round");

        Status = RoundStatus.Active;
        StartedAt = DateTime.UtcNow;
    }

    public void End()
    {
        if (Status != RoundStatus.Active)
            throw new InvalidOperationException("Can only end an active round");

        Status = RoundStatus.Ended;
        EndedAt = DateTime.UtcNow;
    }

    public DateTime GetDeadline()
    {
        if (!StartedAt.HasValue)
            throw new InvalidOperationException("Round has not started yet");

        return StartedAt.Value.AddSeconds(TimeLimitSec);
    }

    public bool IsDeadlinePassed()
    {
        if (!StartedAt.HasValue)
            return false;

        return DateTime.UtcNow >= GetDeadline();
    }

    public int GetElapsedTimeMs()
    {
        if (!StartedAt.HasValue)
            return 0;

        return (int)(DateTime.UtcNow - StartedAt.Value).TotalMilliseconds;
    }

    public void SetWinner(Guid userId)
    {
        WinnerUserId = userId;
    }

    public PlayerAnswer AddAnswer(Guid userId, string answerText, int answerTimeMs)
    {
        if (Status != RoundStatus.Active)
            throw new InvalidOperationException("Can only add answers to an active round");

        if (IsDeadlinePassed())
            throw new InvalidOperationException("Round deadline has passed");

        var answer = new PlayerAnswer(Id, userId, answerText, answerTimeMs);
        _answers.Add(answer);

        return answer;
    }
}
