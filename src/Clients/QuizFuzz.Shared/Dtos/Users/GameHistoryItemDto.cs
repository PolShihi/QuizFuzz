namespace QuizFuzz.Shared.Dtos.Users;

public class GameHistoryItemDto
{
    public Guid SessionId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public int ScoreTotal { get; set; }
    public int CorrectCount { get; set; }
    public int FirstCorrectCount { get; set; }
    public int Rank { get; set; }
    public int PlayersCount { get; set; }
    public bool IsWinner { get; set; }
    public DateTime PlayedAt { get; set; }
    public string SessionStatus { get; set; } = string.Empty;
}
