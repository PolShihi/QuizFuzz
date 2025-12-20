using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Application.Rooms.Queries.GetRoom;

/// <summary>
/// DTO комнаты
/// </summary>
public record RoomDto
{
    public Guid Id { get; init; }
    public Guid OwnerUserId { get; init; }
    public string OwnerUsername { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public RoomVisibility Visibility { get; init; }
    public int MaxPlayers { get; init; }
    public int CurrentPlayers { get; init; }
    public VictoryConditionType VictoryConditionType { get; init; }
    public int VictoryValue { get; init; }
    public TagSelectionMode TagSelectionMode { get; init; }
    public RoomStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<string> Tags { get; init; } = new();
}
