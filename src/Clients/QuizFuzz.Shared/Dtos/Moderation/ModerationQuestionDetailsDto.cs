namespace QuizFuzz.Shared.Dtos.Moderation;

public class ModerationQuestionDetailsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PromptText { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public Guid AuthorUserId { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<ModerationAnswerDto> Answers { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<ModerationHistoryItemDto> ModerationHistory { get; set; } = new();
}

public class ModerationAnswerDto
{
    public Guid Id { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool AllowFuzzyMatch { get; set; }
    public decimal? MinConfidence { get; set; }
    public int? MaxEditDistance { get; set; }
    public List<ModerationAliasDto> Aliases { get; set; } = new();
}

public class ModerationAliasDto
{
    public Guid Id { get; set; }
    public string AliasText { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
}

public class ModerationHistoryItemDto
{
    public Guid ActionId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public Guid ModeratorUserId { get; set; }
    public string ModeratorUsername { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string? Reason { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public DateTime ActionDate { get; set; }
}

