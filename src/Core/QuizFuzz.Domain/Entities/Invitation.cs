using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Приглашение в комнату
/// </summary>
public class Invitation : BaseEntity
{
    public Guid RoomId { get; private set; }
    public string Code { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    // Navigation properties
    public Room Room { get; private set; } = null!;
    public User CreatedBy { get; private set; } = null!;

    private Invitation() { } // EF Core

    public Invitation(Guid roomId, string code, DateTime expiresAt, Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty", nameof(code));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration date must be in the future", nameof(expiresAt));

        RoomId = roomId;
        Code = code;
        ExpiresAt = expiresAt;
        CreatedByUserId = createdByUserId;
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsValid => !IsExpired;
}
