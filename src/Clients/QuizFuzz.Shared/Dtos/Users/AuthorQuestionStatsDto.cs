namespace QuizFuzz.Shared.Dtos.Users;

public class AuthorQuestionStatsDto
{
    public int TotalQuestions { get; set; }
    public int ApprovedQuestions { get; set; }
    public int UnderReviewQuestions { get; set; }
    public int RejectedQuestions { get; set; }
    public int DraftQuestions { get; set; }
    public double ApprovalRate { get; set; }
    public int TimesPlayed { get; set; }
    public int AnswerAttempts { get; set; }
    public int CorrectAnswers { get; set; }
    public double AnswerAccuracy { get; set; }
}
