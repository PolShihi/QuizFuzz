using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.Events;

/// <summary>
/// Событие завершения раунда
/// </summary>
public class RoundEndedEvent : IDomainEvent
{
    public Guid RoundId { get; }
    public Guid SessionId { get; }
    public Guid QuestionId { get; }
    public Guid? WinnerUserId { get; }
    public DateTime OccurredAt { get; }

    public RoundEndedEvent(Guid roundId, Guid sessionId, Guid questionId, Guid? winnerUserId = null)
    {
        RoundId = roundId;
        SessionId = sessionId;
        QuestionId = questionId;
        WinnerUserId = winnerUserId;
        OccurredAt = DateTime.UtcNow;
    }
}
