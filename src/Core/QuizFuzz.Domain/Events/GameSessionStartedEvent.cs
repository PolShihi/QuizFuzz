using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.Events;

/// <summary>
/// Событие старта игровой сессии
/// </summary>
public class GameSessionStartedEvent : IDomainEvent
{
    public Guid SessionId { get; }
    public Guid RoomId { get; }
    public int TotalRoundsPlanned { get; }
    public DateTime OccurredAt { get; }

    public GameSessionStartedEvent(Guid sessionId, Guid roomId, int totalRoundsPlanned)
    {
        SessionId = sessionId;
        RoomId = roomId;
        TotalRoundsPlanned = totalRoundsPlanned;
        OccurredAt = DateTime.UtcNow;
    }
}
