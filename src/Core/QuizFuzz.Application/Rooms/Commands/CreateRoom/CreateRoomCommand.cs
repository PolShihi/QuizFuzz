using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Application.Rooms.Commands.CreateRoom;

/// <summary>
/// Команда создания комнаты
/// </summary>
public record CreateRoomCommand
{
    public string Name { get; init; } = string.Empty;
    public string Visibility { get; init; } = "Public";
    public string? AccessCode { get; init; }
    public int MaxPlayers { get; init; } = 10;
    public string VictoryConditionType { get; init; } = "POINTS";
    public int VictoryValue { get; init; } = 1000;
    public string TagSelectionMode { get; init; } = "Any";
    public List<RoomTagSelectionDto> TagSelections { get; init; } = new();
}

public record RoomTagSelectionDto
{
    public Guid TagId { get; init; }
    public int Weight { get; init; } = 1;
}
