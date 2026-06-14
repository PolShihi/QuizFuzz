namespace QuizFuzz.Shared.Dtos.Rooms;

public class RoomDetailsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OwnerUsername { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public bool IsPrivate { get; set; }
    public int MaxPlayers { get; set; }
    public int RoundTimeLimitSec { get; set; }
    public string Status { get; set; } = string.Empty;
    public string VictoryConditionType { get; set; } = string.Empty;
    public int VictoryValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PlayerInRoomDto> Players { get; set; } = new();
    public Guid? CurrentSessionId { get; set; }
}

public class PlayerInRoomDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public bool IsReady { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool HasActiveSubscription { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
}
