namespace QuizFuzz.Shared.Dtos.Questions;

public class QuestionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PromptText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = "TEXT";
    public string Difficulty { get; set; } = "MEDIUM";
    public string Status { get; set; } = "DRAFT";
    public string LanguageCode { get; set; } = "ru";
    public Guid? AuthorUserId { get; set; }
    public string? AuthorUsername { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<QuestionAnswerDto> Answers { get; set; } = new();
    public List<HintDto> Hints { get; set; } = new();
    public List<TagDto> Tags { get; set; } = new();
    public string? MediaUrl { get; set; }
}

public class QuestionAnswerDto
{
    public Guid Id { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public List<FuzzyAliasDto> Aliases { get; set; } = new();
}

public class FuzzyAliasDto
{
    public Guid Id { get; set; }
    public string AliasText { get; set; } = string.Empty;
    public string Kind { get; set; } = "SYNONYM";
}

public class HintDto
{
    public Guid Id { get; set; }
    public int OrderIndex { get; set; }
    public string? HintText { get; set; }
    public int RevealTimeSeconds { get; set; }
}

public class TagDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
