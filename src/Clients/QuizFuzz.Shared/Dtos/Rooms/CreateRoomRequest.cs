using System.ComponentModel.DataAnnotations;

namespace QuizFuzz.Shared.Dtos.Rooms;

public class CreateRoomRequest
{
    [Required(ErrorMessage = "Room name is required")]
    [MinLength(3, ErrorMessage = "Room name must be at least 3 characters")]
    [MaxLength(100, ErrorMessage = "Room name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    public string Visibility { get; set; } = "Public"; // Public или Private

    [MaxLength(32, ErrorMessage = "Access code cannot exceed 32 characters")]
    public string? AccessCode { get; set; }

    [Range(2, 50, ErrorMessage = "Max players must be between 2 and 50")]
    public int MaxPlayers { get; set; } = 10;

    [Range(10, 300, ErrorMessage = "Round time must be between 10 and 300 seconds")]
    public int RoundTimeLimitSec { get; set; } = 60;

    [Range(3, 20, ErrorMessage = "Number of rounds must be between 3 and 20")]
    public int NumberOfRounds { get; set; } = 10;

    public string VictoryConditionType { get; set; } = "POINTS";

    [Range(1, int.MaxValue, ErrorMessage = "Victory value must be positive")]
    public int VictoryValue { get; set; } = 100;

    public string TagSelectionMode { get; set; } = "Any";

    public List<RoomTagSelectionDto> TagSelections { get; set; } = new();
    
    /// <summary>
    /// Фильтр по сложности вопросов (Easy, Medium, Hard)
    /// Пустой список = все сложности
    /// </summary>
    public List<string> DifficultyFilters { get; set; } = new();
}

public class RoomTagSelectionDto
{
    public Guid TagId { get; set; }
    public int Weight { get; set; } = 1;
}
