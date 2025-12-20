using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Application.Rooms.Commands.CreateRoom;

/// <summary>
/// Команда создания комнаты
/// </summary>
public record CreateRoomCommand
{
    public string Name { get; init; } = string.Empty;
    public RoomVisibility Visibility { get; init; }
    public string? AccessCode { get; init; }
    public int MaxPlayers { get; init; } = 10;
    public VictoryConditionType VictoryConditionType { get; init; }
    public int VictoryValue { get; init; } = 1000;
    public TagSelectionMode TagSelectionMode { get; init; }
    public List<RoomTagSelectionDto> TagSelections { get; init; } = new();
}

public record RoomTagSelectionDto
{
    public Guid TagId { get; init; }
    public int Weight { get; init; } = 1;
}
