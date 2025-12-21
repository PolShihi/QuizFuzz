namespace QuizFuzz.Shared.Dtos.Moderation;

public class ModerationQueueDto
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public string QuestionTitle { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public Guid SubmittedByUserId { get; set; }
    public string SubmittedByUsername { get; set; } = string.Empty;
    public Guid? ModeratorUserId { get; set; }
    public string? ModeratorUsername { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
