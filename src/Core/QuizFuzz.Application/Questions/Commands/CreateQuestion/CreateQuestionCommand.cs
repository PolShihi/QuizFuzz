using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Application.Questions.Commands.CreateQuestion;

/// <summary>
/// Команда создания вопроса
/// </summary>
public record CreateQuestionCommand
{
    public QuestionType Type { get; init; }
    public string? Title { get; init; }
    public string PromptText { get; init; } = string.Empty;
    public Difficulty Difficulty { get; init; }
    public string LanguageCode { get; init; } = "ru";
    public List<CreateAnswerDto> Answers { get; init; } = new();
    public List<Guid> TagIds { get; init; } = new();
    public List<CreateHintDto> Hints { get; init; } = new();
}

public record CreateAnswerDto
{
    public string AnswerText { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public List<CreateAliasDto> Aliases { get; init; } = new();
}

public record CreateAliasDto
{
    public string AliasText { get; init; } = string.Empty;
    public AliasKind Kind { get; init; }
}

public record CreateHintDto
{
    public int OrderIndex { get; init; }
    public string HintText { get; init; } = string.Empty;
    public int RevealTimeSec { get; init; }
}
