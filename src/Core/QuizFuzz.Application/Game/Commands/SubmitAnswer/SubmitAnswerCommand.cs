using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Application.Game.Commands.SubmitAnswer;

/// <summary>
/// Команда отправки ответа игрока
/// </summary>
public record SubmitAnswerCommand
{
    public Guid RoundId { get; init; }
    public string AnswerText { get; init; } = string.Empty;
    public DateTime? ClientTimestamp { get; init; }
}

/// <summary>
/// Результат проверки ответа
/// </summary>
public record SubmitAnswerResult
{
    public Guid PlayerAnswerId { get; init; }
    public bool IsCorrect { get; init; }
    public MatchStrategy Strategy { get; init; }
    public int ScoreAwarded { get; init; }
    public decimal Confidence { get; init; }
    public bool IsFirstCorrect { get; init; }
    public int NewTotalScore { get; init; }
    public int AnswerTimeMs { get; init; }
}
