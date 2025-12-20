namespace QuizFuzz.Application.Game.Queries.GetScoreboard;

/// <summary>
/// DTO таблицы очков
/// </summary>
public record ScoreboardDto
{
    public Guid SessionId { get; init; }
    public List<PlayerScoreDto> Players { get; init; } = new();
}

public record PlayerScoreDto
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public int ScoreTotal { get; init; }
    public int CorrectCount { get; init; }
    public int UniqueCorrectCount { get; init; }
    public int Rank { get; init; }
}
