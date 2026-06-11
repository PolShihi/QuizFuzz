using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuizFuzz.Shared.Dtos.Questions;

public class UpdateQuestionFullRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string PromptText { get; set; } = string.Empty;

    [Required]
    public string Difficulty { get; set; } = "MEDIUM";

    public string? MediaUrl { get; set; }

    [MinLength(1, ErrorMessage = "At least one answer is required")]
    public List<UpdateAnswerRequest> Answers { get; set; } = new();

    public List<Guid> TagIds { get; set; } = new();

    public List<UpdateHintRequest> Hints { get; set; } = new();
}

public class UpdateAnswerRequest
{
    public Guid? Id { get; set; }

    [Required]
    [MinLength(1)]
    public string AnswerText { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public bool AllowFuzzyMatch { get; set; } = true;
    public int? MaxEditDistance { get; set; }
    public decimal? MinConfidence { get; set; }

    [JsonIgnore]
    public decimal? AcceptanceThreshold
    {
        get => MinConfidence;
        set => MinConfidence = value;
    }

    public List<UpdateAliasRequest> Aliases { get; set; } = new();
}

public class UpdateAliasRequest
{
    public Guid? Id { get; set; }

    [Required]
    [MinLength(1)]
    public string AliasText { get; set; } = string.Empty;

    public string Kind { get; set; } = "SYNONYM";
}



public class UpdateHintRequest
{
    public Guid? Id { get; set; }

    public int OrderIndex { get; set; }

    [Required]
    [MinLength(1)]
    public string HintText { get; set; } = string.Empty;

    [Range(0, 300)]
    public int RevealTimeSeconds { get; set; }
}
