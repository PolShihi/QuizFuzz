using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.Events;

/// <summary>
/// Событие завершения игровой сессии
/// </summary>
public class GameSessionFinishedEvent : IDomainEvent
{
    public Guid SessionId { get; }
    public Guid RoomId { get; }
    public int TotalRoundsPlayed { get; }
    public Guid? WinnerUserId { get; }
    public DateTime OccurredAt { get; }

    public GameSessionFinishedEvent(Guid sessionId, Guid roomId, int totalRoundsPlayed, Guid? winnerUserId = null)
    {
        SessionId = sessionId;
        RoomId = roomId;
        TotalRoundsPlayed = totalRoundsPlayed;
        WinnerUserId = winnerUserId;
        OccurredAt = DateTime.UtcNow;
    }
}
