using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.Events;

/// <summary>
/// Событие одобрения вопроса
/// </summary>
public class QuestionApprovedEvent : IDomainEvent
{
    public Guid QuestionId { get; }
    public Guid? ModeratorUserId { get; }
    public DateTime OccurredAt { get; }

    public QuestionApprovedEvent(Guid questionId, Guid? moderatorUserId = null)
    {
        QuestionId = questionId;
        ModeratorUserId = moderatorUserId;
        OccurredAt = DateTime.UtcNow;
    }
}
