using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Application.Common.Interfaces.Services;

/// <summary>
/// Сервис для нечеткого сопоставления ответов
/// </summary>
public interface IFuzzyMatchingService
{
    Task<MatchResult> EvaluateAnswerAsync(
        Guid questionId,
        string userAnswer,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Результат сопоставления ответа
/// </summary>
public record MatchResult
{
    public bool IsCorrect { get; init; }
    public MatchStrategy Strategy { get; init; }
    public decimal Confidence { get; init; }
    public string NormalizedAnswer { get; init; } = string.Empty;
    public Guid? MatchedQuestionAnswerId { get; init; }
    public Guid? MatchedAliasId { get; init; }
}
