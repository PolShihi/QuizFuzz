using Microsoft.EntityFrameworkCore;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Infrastructure.Persistence.Repositories;

/// <summary>
/// Репозиторий для работы с вопросами
/// </summary>
public class QuestionRepository : BaseRepository<Question>, IQuestionRepository
{
    public QuestionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Question?> GetWithAnswersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    public async Task<Question?> GetWithAllDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsSplitQuery()
            .Include(q => q.Answers)
                .ThenInclude(a => a.Aliases)
            .Include(q => q.Hints)
            .Include(q => q.MediaAssets)
            .Include(q => q.Tags)
                .ThenInclude(qt => qt.Tag)
            .Include(q => q.Author)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Question>> GetByStatusAsync(
        QuestionStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(q => q.Status == status)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Question>> GetForModerationQueueAsync(
        QuestionStatus? status = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            limit = 100;
        }

        var query = _dbSet
            .Include(q => q.Author)
            .Include(q => q.Answers)
            .Include(q => q.Tags)
                .ThenInclude(qt => qt.Tag)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(q => q.Status == status.Value);
        }

        return await query
            .OrderByDescending(q => q.UpdatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Question>> GetByTagsAsync(
        IEnumerable<Guid> tagIds,
        CancellationToken cancellationToken = default)
    {
        var tagIdsList = tagIds.ToList();

        return await _dbSet
            .Include(q => q.Tags)
            .Where(q => q.Tags.Any(qt => tagIdsList.Contains(qt.TagId)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Question>> GetApprovedByTagsAsync(
        IEnumerable<Guid> tagIds,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var tagIdsList = tagIds.ToList();

        return await _dbSet
            .Include(q => q.Tags)
            .Where(q => q.Status == QuestionStatus.Approved)
            .Where(q => q.Tags.Any(qt => tagIdsList.Contains(qt.TagId)))
            .OrderByDescending(q => q.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<Question?> GetRandomApprovedAsync(
        IEnumerable<Guid>? tagIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(q => q.Status == QuestionStatus.Approved);

        if (tagIds != null)
        {
            var tagIdsList = tagIds.ToList();
            query = query.Where(q => q.Tags.Any(qt => tagIdsList.Contains(qt.TagId)));
        }

        // PostgreSQL specific: ORDER BY RANDOM()
        return await query
            .OrderBy(q => Guid.NewGuid()) // EF Core will translate this
            .FirstOrDefaultAsync(cancellationToken);
    }
    
    public async Task<Question?> GetRandomApprovedWithFiltersAsync(
        IEnumerable<Guid>? tagIds = null,
        IEnumerable<Difficulty>? difficulties = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(q => q.Answers)
                .ThenInclude(a => a.Aliases)
            .Include(q => q.Hints)
            .Include(q => q.MediaAssets)  //  КРИТИЧНО: загружаем MediaAssets для IMAGE/AUDIO!
            .Include(q => q.Tags)
                .ThenInclude(qt => qt.Tag)
            .Where(q => q.Status == QuestionStatus.Approved);

        // Логируем начальное количество вопросов
        var totalApproved = await _dbSet.CountAsync(q => q.Status == QuestionStatus.Approved, cancellationToken);

        // Фильтр по тегам (OR логика - хотя бы один тег совпадает)
        if (tagIds != null && tagIds.Any())
        {
            var tagIdsList = tagIds.ToList();
            foreach (var tagId in tagIdsList)
            {
            }
            
            query = query.Where(q => q.Tags.Any(qt => tagIdsList.Contains(qt.TagId)));
            
            var countWithTags = await query.CountAsync(cancellationToken);
        }
        else
        {
        }

        // Фильтр по сложности (OR логика - хотя бы одна сложность совпадает)
        if (difficulties != null && difficulties.Any())
        {
            var difficultiesList = difficulties.ToList();
            foreach (var diff in difficultiesList)
            {
            }
            
            query = query.Where(q => difficultiesList.Contains(q.Difficulty));
            
            var countWithDifficulty = await query.CountAsync(cancellationToken);
        }
        else
        {
        }

        // Случайный выбор
        var result = await query
            .OrderBy(q => Guid.NewGuid())
            .FirstOrDefaultAsync(cancellationToken);
            
        if (result != null)
        {
            if (result.MediaAssets.Any())
            {
                foreach (var media in result.MediaAssets)
                {
                }
            }
            foreach (var qt in result.Tags)
            {
            }
        }
        else
        {
        }
        
        return result;
    }
}
