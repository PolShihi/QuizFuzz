using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.Events;

/// <summary>
/// Событие первого правильного ответа в раунде
/// </summary>
public class FirstCorrectAnswerEvent : IDomainEvent
{
    public Guid RoundId { get; }
    public Guid UserId { get; }
    public Guid PlayerAnswerId { get; }
    public int AnswerTimeMs { get; }
    public DateTime OccurredAt { get; }

    public FirstCorrectAnswerEvent(Guid roundId, Guid userId, Guid playerAnswerId, int answerTimeMs)
    {
        RoundId = roundId;
        UserId = userId;
        PlayerAnswerId = playerAnswerId;
        AnswerTimeMs = answerTimeMs;
        OccurredAt = DateTime.UtcNow;
    }
}
