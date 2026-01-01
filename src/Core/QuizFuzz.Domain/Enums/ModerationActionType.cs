namespace QuizFuzz.Domain.Enums;

/// <summary>
/// Тип действия модератора
/// </summary>
public enum ModerationActionType
{
    /// <summary>
    /// Одобрить вопрос
    /// </summary>
    Approve = 1,

    /// <summary>
    /// Отклонить вопрос
    /// </summary>
    Reject = 2,

    /// <summary>
    /// Запросить изменения
    /// </summary>
    RequestChanges = 3,

    /// <summary>
    /// Архивировать вопрос
    /// </summary>
    Archive = 4,

    /// <summary>
    /// Восстановить вопрос
    /// </summary>
    Restore = 5
}
