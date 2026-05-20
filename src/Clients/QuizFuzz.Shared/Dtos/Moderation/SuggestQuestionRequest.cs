using System.ComponentModel.DataAnnotations;
using QuizFuzz.Shared.Enums;

namespace QuizFuzz.Shared.Dtos.Moderation;

public class SuggestQuestionRequest
{
    [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Question text is required")]
    [MinLength(10, ErrorMessage = "Question text must be at least 10 characters")]
    [MaxLength(2000, ErrorMessage = "Question text cannot exceed 2000 characters")]
    public string PromptText { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public Difficulty Difficulty { get; set; }
    [MinLength(1, ErrorMessage = "At least one correct answer is required")]
    [MaxLength(10, ErrorMessage = "A question can have at most 10 correct answers")]
    public List<AnswerWithSettings> CorrectAnswers { get; set; } = new();

    [MaxLength(8, ErrorMessage = "A question can have at most 8 tags")]
    public List<Guid> TagIds { get; set; } = new();

    [MaxLength(2048, ErrorMessage = "Media URL is too long")]
    public string? MediaUrl { get; set; }

    [MaxLength(1000, ErrorMessage = "Explanation cannot exceed 1000 characters")]
    public string? Explanation { get; set; }
}

public class AnswerWithSettings
{
    [Required(ErrorMessage = "Answer text is required")]
    [MaxLength(200, ErrorMessage = "Answer text cannot exceed 200 characters")]
    public string Text { get; set; } = string.Empty;

    public bool AllowFuzzyMatch { get; set; } = true;

    [Range(0.0, 1.0, ErrorMessage = "Min confidence must be between 0 and 1")]
    public double MinConfidence { get; set; } = 0.7;
}
