using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Application.Common.Interfaces.Persistence;

/// <summary>
/// Репозиторий для работы с действиями модерации
/// </summary>
public interface IModerationActionRepository : IRepository<ModerationAction>
{
    /// <summary>
    /// Получить все действия модерации для конкретного вопроса
    /// </summary>
    Task<IReadOnlyList<ModerationAction>> GetByQuestionIdAsync(
        Guid questionId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить все действия конкретного модератора
    /// </summary>
    Task<IReadOnlyList<ModerationAction>> GetByModeratorIdAsync(
        Guid moderatorId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить последнее действие модерации для вопроса
    /// </summary>
    Task<ModerationAction?> GetLatestByQuestionIdAsync(
        Guid questionId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить действия модерации по типу
    /// </summary>
    Task<IReadOnlyList<ModerationAction>> GetByActionTypeAsync(
        ModerationActionType actionType, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить статистику модерации для модератора
    /// </summary>
    Task<ModeratorStats> GetModeratorStatsAsync(
        Guid moderatorId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Статистика модератора
/// </summary>
public class ModeratorStats
{
    public int TotalActions { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int RequestChangesCount { get; set; }
    public DateTime? LastActionDate { get; set; }
}
