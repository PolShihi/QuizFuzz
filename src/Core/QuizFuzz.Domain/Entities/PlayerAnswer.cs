using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Ответ игрока на вопрос в раунде
/// </summary>
public class PlayerAnswer : BaseEntity
{
    public Guid RoundId { get; private set; }
    public Guid UserId { get; private set; }
    public string AnswerText { get; private set; }
    public int AnswerTimeMs { get; private set; }
    public DateTime? ClientTimestamp { get; private set; }

    // Navigation properties
    public GameRound Round { get; private set; } = null!;
    public User User { get; private set; } = null!;
    public AnswerEvaluation? Evaluation { get; private set; }

    private PlayerAnswer() { } // EF Core

    public PlayerAnswer(
        Guid roundId,
        Guid userId,
        string answerText,
        int answerTimeMs,
        DateTime? clientTimestamp = null)
    {
        if (string.IsNullOrWhiteSpace(answerText))
            throw new ArgumentException("Answer text cannot be empty", nameof(answerText));

        if (answerTimeMs < 0)
            throw new ArgumentException("Answer time cannot be negative", nameof(answerTimeMs));

        RoundId = roundId;
        UserId = userId;
        AnswerText = answerText.Trim();
        AnswerTimeMs = answerTimeMs;
        ClientTimestamp = clientTimestamp;
    }

    public void SetEvaluation(AnswerEvaluation evaluation)
    {
        if (Evaluation != null)
            throw new InvalidOperationException("Answer has already been evaluated");

        Evaluation = evaluation;
    }
}
