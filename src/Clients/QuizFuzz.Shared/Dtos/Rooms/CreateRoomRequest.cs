using System.ComponentModel.DataAnnotations;

namespace QuizFuzz.Shared.Dtos.Rooms;

public class CreateRoomRequest
{
    [Required(ErrorMessage = "Room name is required")]
    [MinLength(3, ErrorMessage = "Room name must be at least 3 characters")]
    [MaxLength(100, ErrorMessage = "Room name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    public bool IsPrivate { get; set; }

    [Range(2, 50, ErrorMessage = "Max players must be between 2 and 50")]
    public int MaxPlayers { get; set; } = 10;

    public string VictoryConditionType { get; set; } = "POINTS";

    [Range(1, int.MaxValue, ErrorMessage = "Victory value must be positive")]
    public int VictoryValue { get; set; } = 100;

    public List<int> TagIds { get; set; } = new();
}
