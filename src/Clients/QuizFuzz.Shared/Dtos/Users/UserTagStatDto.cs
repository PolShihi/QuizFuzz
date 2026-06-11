namespace QuizFuzz.Shared.Dtos.Users;

public class UserTagStatDto
{
    public Guid TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public int QuestionsAnswered { get; set; }
    public int CorrectAnswers { get; set; }
    public double Accuracy { get; set; }
}
