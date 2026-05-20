using QuizFuzz.Shared.Enums;

namespace QuizFuzz.Shared.Dtos.Moderation;

public class SuggestQuestionRequest
{
    public string? Title { get; set; }
    public string PromptText { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public Difficulty Difficulty { get; set; }
    public List<AnswerWithSettings> CorrectAnswers { get; set; } = new();
    public List<Guid> TagIds { get; set; } = new();
    public string? MediaUrl { get; set; }
    public string? Explanation { get; set; }
}

public class AnswerWithSettings
{
    public string Text { get; set; } = string.Empty;
    public bool AllowFuzzyMatch { get; set; } = true;
    public double MinConfidence { get; set; } = 0.7;
}
