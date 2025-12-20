using QuizFuzz.Domain.Common;
using QuizFuzz.Domain.Enums;
using QuizFuzz.Domain.ValueObjects;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Оценка ответа игрока (1-1 с PlayerAnswer)
/// </summary>
public class AnswerEvaluation : BaseEntity
{
    public Guid PlayerAnswerId { get; private set; }
    public bool IsCorrect { get; private set; }
    public MatchStrategy MatchStrategy { get; private set; }
    public int ScoreAwarded { get; private set; }
    public decimal ConfidenceValue { get; private set; }
    public string NormalizedAnswer { get; private set; }
    public Guid? MatchedQuestionAnswerId { get; private set; }
    public Guid? MatchedAliasId { get; private set; }
    public DateTime EvaluatedAt { get; private set; }

    // Navigation properties
    public PlayerAnswer PlayerAnswer { get; private set; } = null!;
    public QuestionAnswer? MatchedQuestionAnswer { get; private set; }
    public FuzzyAlias? MatchedAlias { get; private set; }

    private AnswerEvaluation() { } // EF Core

    public AnswerEvaluation(
        Guid playerAnswerId,
        bool isCorrect,
        MatchStrategy matchStrategy,
        int scoreAwarded,
        Confidence confidence,
        string normalizedAnswer,
        Guid? matchedQuestionAnswerId = null,
        Guid? matchedAliasId = null)
    {
        if (scoreAwarded < 0)
            throw new ArgumentException("Score awarded cannot be negative", nameof(scoreAwarded));

        if (string.IsNullOrWhiteSpace(normalizedAnswer))
            throw new ArgumentException("Normalized answer cannot be empty", nameof(normalizedAnswer));

        PlayerAnswerId = playerAnswerId;
        IsCorrect = isCorrect;
        MatchStrategy = matchStrategy;
        ScoreAwarded = scoreAwarded;
        ConfidenceValue = confidence.Value;
        NormalizedAnswer = normalizedAnswer;
        MatchedQuestionAnswerId = matchedQuestionAnswerId;
        MatchedAliasId = matchedAliasId;
        EvaluatedAt = DateTime.UtcNow;
    }

    public Confidence GetConfidence() => Confidence.Create(ConfidenceValue);
}
