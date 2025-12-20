using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Игрок в комнате (сессии)
/// </summary>
public class PlayerInRoom : BaseEntity
{
    public Guid SessionId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? LeftAt { get; private set; }
    public bool IsOwnerSnapshot { get; private set; }

    // Navigation properties
    public GameSession Session { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private PlayerInRoom() { } // EF Core

    public PlayerInRoom(Guid sessionId, Guid userId, bool isOwner = false)
    {
        SessionId = sessionId;
        UserId = userId;
        JoinedAt = DateTime.UtcNow;
        IsOwnerSnapshot = isOwner;
    }

    public void Leave()
    {
        if (LeftAt.HasValue)
            throw new InvalidOperationException("Player has already left");

        LeftAt = DateTime.UtcNow;
    }

    public bool IsActive => !LeftAt.HasValue;
}
