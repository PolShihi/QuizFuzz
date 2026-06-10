namespace QuizFuzz.Shared.Dtos.Game;

public class GameQuestionDto
{
    public Guid QuestionId { get; set; }
    public Guid RoundId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public string QuestionType { get; set; } = "TEXT";
    public int TimeLimit { get; set; }
    public DateTime StartedAt { get; set; }
    public List<HintDto> Hints { get; set; } = new();
}

public class HintDto
{
    public Guid Id { get; set; }
    public int OrderIndex { get; set; }
    public string? Text { get; set; }
    public string? MediaUrl { get; set; }
    public int RevealTimeSeconds { get; set; }
}
