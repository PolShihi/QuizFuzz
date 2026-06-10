namespace QuizFuzz.Shared.Dtos.Game;

public class RevealedHintDto
{
    public Guid Id { get; set; }
    public int OrderIndex { get; set; }
    public string? Text { get; set; }
    public string? MediaUrl { get; set; }
    public int RevealTimeSeconds { get; set; }
    public int RevealedAtSeconds { get; set; }
}
