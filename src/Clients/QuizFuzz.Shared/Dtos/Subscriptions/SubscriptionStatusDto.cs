namespace QuizFuzz.Shared.Dtos.Subscriptions;

public class SubscriptionStatusDto
{
    public bool IsActive { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}
