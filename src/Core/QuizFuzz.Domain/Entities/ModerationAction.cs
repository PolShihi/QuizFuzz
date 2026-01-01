using QuizFuzz.Domain.Common;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Действие модератора над вопросом
/// </summary>
public class ModerationAction : BaseEntity
{
    /// <summary>
    /// ID вопроса, который модерируется
    /// </summary>
    public Guid QuestionId { get; private set; }

    /// <summary>
    /// Навигационное свойство к вопросу
    /// </summary>
    public Question Question { get; private set; } = null!;

    /// <summary>
    /// ID модератора, который выполнил действие
    /// </summary>
    public Guid ModeratorUserId { get; private set; }

    /// <summary>
    /// Навигационное свойство к модератору
    /// </summary>
    public User Moderator { get; private set; } = null!;

    /// <summary>
    /// Действие (Approve, Reject, RequestChanges)
    /// </summary>
    public ModerationActionType ActionType { get; private set; }

    /// <summary>
    /// Комментарий модератора
    /// </summary>
    public string? Comment { get; private set; }

    /// <summary>
    /// Причина отклонения (если применимо)
    /// </summary>
    public string? Reason { get; private set; }

    /// <summary>
    /// Предыдущий статус вопроса
    /// </summary>
    public QuestionStatus PreviousStatus { get; private set; }

    /// <summary>
    /// Новый статус вопроса
    /// </summary>
    public QuestionStatus NewStatus { get; private set; }

    /// <summary>
    /// Дата выполнения действия
    /// </summary>
    public DateTime ActionDate { get; private set; }

    // Private constructor for EF Core
    private ModerationAction() { }

    /// <summary>
    /// Создать новое действие модерации
    /// </summary>
    public static ModerationAction Create(
        Question question,
        User moderator,
        ModerationActionType actionType,
        QuestionStatus previousStatus,
        QuestionStatus newStatus,
        string? comment = null,
        string? reason = null)
    {
        if (question == null)
            throw new ArgumentNullException(nameof(question));
        
        if (moderator == null)
            throw new ArgumentNullException(nameof(moderator));

        if (!moderator.IsModerator && !moderator.IsAdmin)
            throw new InvalidOperationException("User is not a moderator or admin");

        return new ModerationAction
        {
            Id = Guid.NewGuid(),
            QuestionId = question.Id,
            Question = question,
            ModeratorUserId = moderator.Id,
            Moderator = moderator,
            ActionType = actionType,
            Comment = comment,
            Reason = reason,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ActionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Обновить комментарий
    /// </summary>
    public void UpdateComment(string comment)
    {
        Comment = comment;
    }
}
