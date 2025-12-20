namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Выбор тегов для комнаты (many-to-many с весами)
/// </summary>
public class RoomTagSelection
{
    public Guid RoomId { get; private set; }
    public Guid TagId { get; private set; }
    public int Weight { get; private set; }

    // Navigation properties
    public Room Room { get; private set; } = null!;
    public Tag Tag { get; private set; } = null!;

    private RoomTagSelection() { } // EF Core

    public RoomTagSelection(Guid roomId, Guid tagId, int weight = 1)
    {
        if (weight <= 0)
            throw new ArgumentException("Weight must be positive", nameof(weight));

        RoomId = roomId;
        TagId = tagId;
        Weight = weight;
    }

    public void UpdateWeight(int weight)
    {
        if (weight <= 0)
            throw new ArgumentException("Weight must be positive", nameof(weight));

        Weight = weight;
    }
}
