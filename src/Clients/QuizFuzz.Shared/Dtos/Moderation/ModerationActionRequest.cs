using System.ComponentModel.DataAnnotations;

namespace QuizFuzz.Shared.Dtos.Moderation;

public class ModerationActionRequest
{
    [Required]
    public Guid QueueId { get; set; }

    [Required]
    public string Action { get; set; } = "APPROVE"; // APPROVE, REJECT, EDIT

    public string? Comment { get; set; }

    public string? Reason { get; set; }
}
