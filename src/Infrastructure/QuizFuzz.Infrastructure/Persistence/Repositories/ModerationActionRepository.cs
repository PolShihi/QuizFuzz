using Microsoft.EntityFrameworkCore;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Infrastructure.Persistence.Repositories;

/// <summary>
/// Репозиторий для работы с действиями модерации
/// </summary>
public class ModerationActionRepository : BaseRepository<ModerationAction>, IModerationActionRepository
{
    public ModerationActionRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Получить все действия модерации для конкретного вопроса
    /// </summary>
    public async Task<IReadOnlyList<ModerationAction>> GetByQuestionIdAsync(
        Guid questionId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ModerationAction>()
            .Where(ma => ma.QuestionId == questionId)
            .Include(ma => ma.Moderator)
            .Include(ma => ma.Question)
            .OrderByDescending(ma => ma.ActionDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получить все действия конкретного модератора
    /// </summary>
    public async Task<IReadOnlyList<ModerationAction>> GetByModeratorIdAsync(
        Guid moderatorId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ModerationAction>()
            .Where(ma => ma.ModeratorUserId == moderatorId)
            .Include(ma => ma.Question)
            .OrderByDescending(ma => ma.ActionDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получить последнее действие модерации для вопроса
    /// </summary>
    public async Task<ModerationAction?> GetLatestByQuestionIdAsync(
        Guid questionId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ModerationAction>()
            .Where(ma => ma.QuestionId == questionId)
            .Include(ma => ma.Moderator)
            .OrderByDescending(ma => ma.ActionDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Получить действия модерации по типу
    /// </summary>
    public async Task<IReadOnlyList<ModerationAction>> GetByActionTypeAsync(
        ModerationActionType actionType, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ModerationAction>()
            .Where(ma => ma.ActionType == actionType)
            .Include(ma => ma.Moderator)
            .Include(ma => ma.Question)
            .OrderByDescending(ma => ma.ActionDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получить статистику модерации для модератора
    /// </summary>
    public async Task<ModeratorStats> GetModeratorStatsAsync(
        Guid moderatorId, 
        CancellationToken cancellationToken = default)
    {
        var actions = await _context.Set<ModerationAction>()
            .Where(ma => ma.ModeratorUserId == moderatorId)
            .ToListAsync(cancellationToken);

        return new ModeratorStats
        {
            TotalActions = actions.Count,
            ApprovedCount = actions.Count(a => a.ActionType == ModerationActionType.Approve),
            RejectedCount = actions.Count(a => a.ActionType == ModerationActionType.Reject),
            RequestChangesCount = actions.Count(a => a.ActionType == ModerationActionType.RequestChanges),
            LastActionDate = actions.Any() ? actions.Max(a => a.ActionDate) : null
        };
    }
}
