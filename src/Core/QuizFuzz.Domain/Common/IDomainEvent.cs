namespace QuizFuzz.Domain.Common;

/// <summary>
/// Интерфейс для доменных событий
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
