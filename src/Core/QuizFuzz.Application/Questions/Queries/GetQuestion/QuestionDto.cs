using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Application.Questions.Queries.GetQuestion;

/// <summary>
/// DTO вопроса
/// </summary>
public record QuestionDto
{
    public Guid Id { get; init; }
    public QuestionType Type { get; init; }
    public string? Title { get; init; }
    public string PromptText { get; init; } = string.Empty;
    public Difficulty Difficulty { get; init; }
    public string LanguageCode { get; init; } = string.Empty;
    public QuestionStatus Status { get; init; }
    public Guid? AuthorUserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<AnswerDto> Answers { get; init; } = new();
    public List<HintDto> Hints { get; init; } = new();
    public List<TagDto> Tags { get; init; } = new();
}

public record AnswerDto
{
    public Guid Id { get; init; }
    public string AnswerText { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public bool IsActive { get; init; }
    public List<AliasDto> Aliases { get; init; } = new();
}

public record AliasDto
{
    public Guid Id { get; init; }
    public string AliasText { get; init; } = string.Empty;
    public AliasKind Kind { get; init; }
}

public record HintDto
{
    public Guid Id { get; init; }
    public int OrderIndex { get; init; }
    public string HintText { get; init; } = string.Empty;
    public int RevealTimeSec { get; init; }
}

public record TagDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
