using System.ComponentModel.DataAnnotations;

namespace QuizFuzz.Shared.Dtos.Questions;

public class CreateQuestionRequest
{
    [Required(ErrorMessage = "Title is required")]
    [MinLength(5, ErrorMessage = "Title must be at least 5 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Question text is required")]
    [MinLength(10, ErrorMessage = "Question text must be at least 10 characters")]
    public string PromptText { get; set; } = string.Empty;

    [Required]
    public string QuestionType { get; set; } = "TEXT";

    [Required]
    public string Difficulty { get; set; } = "MEDIUM";

    public string LanguageCode { get; set; } = "ru";

    public string? MediaUrl { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one answer is required")]
    public List<CreateAnswerRequest> Answers { get; set; } = new();

    public List<CreateHintRequest> Hints { get; set; } = new();

    public List<int> TagIds { get; set; } = new();
}

public class CreateAnswerRequest
{
    [Required]
    [MinLength(1)]
    public string AnswerText { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public List<CreateAliasRequest> Aliases { get; set; } = new();
}

public class CreateAliasRequest
{
    [Required]
    [MinLength(1)]
    public string AliasText { get; set; } = string.Empty;

    public string Kind { get; set; } = "SYNONYM";
}

public class CreateHintRequest
{
    public int OrderIndex { get; set; }

    [Required]
    public string HintText { get; set; } = string.Empty;

    [Range(0, 300)]
    public int RevealTimeSeconds { get; set; }
}
