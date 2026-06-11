namespace QuizFuzz.Shared.Dtos.Users;

public class GameHistoryResponseDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<GameHistoryItemDto> Items { get; set; } = new();
}
