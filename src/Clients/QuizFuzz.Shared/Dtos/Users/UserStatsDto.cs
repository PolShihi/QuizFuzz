namespace QuizFuzz.Shared.Dtos.Users;

public class UserStatsDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int GamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public int QuestionsAnswered { get; set; }
    public int UniqueFirstCorrect { get; set; }
    public int AverageAnswerTimeMs { get; set; }
    public double WinRate => GamesPlayed > 0 ? (double)GamesWon / GamesPlayed * 100 : 0;
    public double Accuracy => QuestionsAnswered > 0 ? (double)UniqueFirstCorrect / QuestionsAnswered * 100 : 0;
}
